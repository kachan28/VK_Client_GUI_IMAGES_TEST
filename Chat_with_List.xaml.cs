using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Linq;
using VkNet;
using VkNet.Model;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.RequestParams;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto;
using System.Windows.Documents;
using System.Windows;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Image = System.Windows.Controls.Image;
using System.IO;
using System.Windows.Media.Imaging;
using System.Net;
using System.Windows.Media;
using VkNet.Model.Attachments;


namespace VKMessenger_by_MK
{
    /// <summary>
    /// Логика взаимодействия для Chat_with_List.xaml
    /// </summary>
    public partial class Chat_with_List : System.Windows.Controls.Page
    {
        int idsob;

        public static string pubkey = MainWindow.pubkey;

        public Chat_with_List()
        {
            InitializeComponent();
            CreateFriendsList();
        }



        public void CreateFriendsList()
        {
            FullFriendList.Items.Add(MainWindow.api.Account.GetProfileInfo().FirstName + " " + MainWindow.api.Account.GetProfileInfo().LastName + " ID:" + MainWindow.api.UserId);

            var friend_list = MainWindow.api.Friends.Get(new FriendsGetParams
            {
                Order = FriendsOrder.Hints,
                Fields = ProfileFields.FirstName,
                Count = 6000,
                NameCase = NameCase.Nom
            });
            foreach (var friend in friend_list)
            {
                FullFriendList.Items.Add(friend.FirstName + " " + friend.LastName + " ID:" + friend.Id);
            }
        }

        private void IDSearch_Is_Focused(object sender, System.Windows.RoutedEventArgs e)
        {
            this.IDSearch.Text = "";
            this.IDSearch.Foreground = System.Windows.Media.Brushes.Black;
        }

        private void IDSearch_Lost_Focus(object sender, System.Windows.RoutedEventArgs e)
        {
            this.IDSearch.Text = "Find person with ID";
            this.IDSearch.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void ID_Entered(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {

                idsob = Convert.ToInt32(IDSearch.Text);
                var StartDialogThread = new Thread(() => StartDialog(idsob, MainWindow.api, ref pubkey, MainWindow.privkey, MainWindow.SimKeyforMes));
                StartDialogThread.Start();

            }
        }

        private void ElementSelected(object sender, MouseButtonEventArgs e)
        {

            string userdata = FullFriendList.SelectedValue.ToString();
            idsob = Convert.ToInt32(userdata.Substring(userdata.IndexOf("ID:") + 3));
            var StartDialogThread = new Thread(() => StartDialog(idsob, MainWindow.api, ref pubkey, MainWindow.privkey, MainWindow.SimKeyforMes));
            StartDialogThread.Start();
        }

        private void StartDialog(int idsob, VkApi api, ref string pubkey, string privkey, string SimKey)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Clear();
            }));

            var GetMesThread = new Thread(Get_Mes);
            bool me_or_him = true;

            Random random = new Random();

            if (!Check_Key_Without_GUI(MainWindow.api, idsob))
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = idsob,
                        RandomId = random.Next(99999),
                        Message = "Using VKMessenger by MK"
                    });
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Chat.Document.Blocks.Add(new Paragraph(new Run("Using VKMessenger by MK")));
                    }));
                }
                catch
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams
                        {
                            UserId = idsob,
                            RandomId = random.Next(99999),
                            Message = "Using VKMessenger by MK"
                        });
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            Chat.Document.Blocks.Add(new Paragraph(new Run("Using VKMessenger by MK")));
                        }));
                    }
                    catch (Exception e)
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            Chat.Document.Blocks.Add(new Paragraph(new Run(e.ToString())));
                        }));
                    }
                }
            }

            var newpubkey = ChangeKeys(api, pubkey, idsob, ref me_or_him);
            pubkey = newpubkey;

            if (me_or_him)
            {
                Send_Sim_Key(api, idsob, SimKey, pubkey);
            }
            else
            {
                SimKey = Get_Sim_Key(api, idsob, privkey);
                MainWindow.SimKeyforMes = SimKey;
            }

            string npub = pubkey;

            object mesargums = new object[] { api, idsob, SimKey, pubkey, privkey };

            GetMesThread.SetApartmentState(ApartmentState.STA);
            GetMesThread.IsBackground = true;
            GetMesThread.Start(mesargums);
        }

        private void Send_Sim_Key(VkApi api, int idsob, string SimKey, string pubkey)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Идет отправка симметричного ключа...")));
            }));
            var random = new Random();
            var randid = random.Next(99999);
            var CryptedSimKey = RSAEncryption(SimKey, pubkey);
            api.Messages.Send(new MessagesSendParams
            {
                UserId = idsob,
                RandomId = randid,
                Message = CryptedSimKey
            });
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ успешно отправлен!!!")));
            }));
        }

        private string Get_Sim_Key(VkApi api, int idsob, string privkey)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Получаем ключ симметричного шифрования...")));
            }));
            string newkey;
            while (true)
            {
#pragma warning disable CS0618 // Тип или член устарел
                MessagesGetObject getDialogs = api.Messages.GetDialogs(new MessagesDialogsGetParams
                {
                    Count = 200
                });
#pragma warning restore CS0618 // Тип или член устарел
                Thread.Sleep(500);
                var curmessage = "";
                var state = false;
                int i;
                for (i = 0; i < 200; i++)
                {
                    if (getDialogs.Messages[i].UserId == idsob)
                    {
                        state = (bool)getDialogs.Messages[i].Out;
                        curmessage = getDialogs.Messages[i].Body;
                        break;
                    }
                }

                if (state == false && curmessage.Substring(0, 13) != "<RSAKeyValue>")
                {
                    newkey = RSADecryption(curmessage, privkey);
                    break;
                }
            }

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ получен!!!")));
            }));
            return newkey;
        }

        public static string RSAEncryption(string strText, string pubkey)
        {
            var publicKey = pubkey;

            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                try
                {
                    // client encrypting data with public key issued by server                    
                    FromXmlString(rsa, publicKey);

                    var encryptedData = rsa.Encrypt(testData, true);

                    var base64Encrypted = Convert.ToBase64String(encryptedData);

                    return base64Encrypted;
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        public static string RSADecryption(string strText, string privkey)
        {
            var privateKey = privkey;

            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                try
                {
                    var base64Encrypted = strText;

                    // server decrypting data with private key                    
                    FromXmlString(rsa, privateKey);

                    var npriv = privateKey;

                    Console.WriteLine(ToXmlString(rsa, true));

                    var resultBytes = Convert.FromBase64String(base64Encrypted);
                    var decryptedBytes = rsa.Decrypt(resultBytes, true);
                    var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                    return decryptedData.ToString();
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        private static void FromXmlString(RSA rsa, string xmlString)
        {
            RSAParameters parameters = new RSAParameters();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus":
                            parameters.Modulus = (string.IsNullOrEmpty(node.InnerText)
                                ? null
                                : Convert.FromBase64String(node.InnerText));
                            break;
                        case "Exponent":
                            parameters.Exponent = (string.IsNullOrEmpty(node.InnerText)
                                ? null
                                : Convert.FromBase64String(node.InnerText));
                            break;
                        case "P":
                            parameters.P = (string.IsNullOrEmpty(node.InnerText)
                                ? null
                                : Convert.FromBase64String(node.InnerText));
                            break;
                        case "Q":
                            parameters.Q = (string.IsNullOrEmpty(node.InnerText)
                                ? null
                                : Convert.FromBase64String(node.InnerText));
                            break;
                        case "DP":
                            parameters.DP = (string.IsNullOrEmpty(node.InnerText)
                                ? null
                                : Convert.FromBase64String(node.InnerText));
                            break;
                        case "DQ":
                            parameters.DQ = (string.IsNullOrEmpty(node.InnerText)
                                ? null
                                : Convert.FromBase64String(node.InnerText));
                            break;
                        case "InverseQ":
                            parameters.InverseQ = (string.IsNullOrEmpty(node.InnerText)
                                ? null
                                : Convert.FromBase64String(node.InnerText));
                            break;
                        case "D":
                            parameters.D = (string.IsNullOrEmpty(node.InnerText)
                                ? null
                                : Convert.FromBase64String(node.InnerText));
                            break;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            rsa.ImportParameters(parameters);
        }

        private static string ToXmlString(RSA rsa, bool includePrivateParameters)
        {
            RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

            return string.Format(
                "<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null,
                parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null,
                parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
                parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
                parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null,
                parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null,
                parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null,
                parameters.D != null ? Convert.ToBase64String(parameters.D) : null);
        }

        private void Send_Message(VkApi api, int idsob, string fullmessage)
        {
            Random random = new Random();
            int randid = random.Next(999999);
            string pattern = "<Image>";
            string message = "";
            string date = DateTime.Now.ToString();

            Regex regex = new Regex(pattern);

            Match match = Regex.Match(fullmessage, pattern);
            if (match.Length > 0)
            {
                message = fullmessage.Substring(0, match.Index);
            }
            else
            {
                message = fullmessage;
            }
            message += "<VkMKDateMes>" + date;
            string crmessage = Encryption(message, MainWindow.SimKeyforMes);
            string attachmentstr = "", crattachments = "";
            string ImageFilePath = Environment.CurrentDirectory;

            if (match.Length > 0)
            {
                attachmentstr = fullmessage.Substring(match.Index, fullmessage.Length - message.Length);
                crattachments = Encryption(attachmentstr, MainWindow.SimKeyforMes);
                File.WriteAllText(Path.Combine(ImageFilePath, "Images.txt"), string.Empty);
                File.WriteAllText(Path.Combine(ImageFilePath, "Images.txt"), crattachments);

                UploadServerInfo uploadServer = MainWindow.api.Docs.GetUploadServer();
                // Загрузить файл.
                WebClient wc = new WebClient();
                string responseFile = Encoding.ASCII.GetString(wc.UploadFile(uploadServer.UploadUrl, Path.Combine(ImageFilePath, "Images.txt")));
                // Сохранить загруженный файл
                var attachments = MainWindow.api.Docs.Save(responseFile, "doc").Select(x => x.Instance);


                try
                {
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = idsob,
                        RandomId = randid,
                        Message = crmessage,
                        Attachments = attachments
                    });
                }
                catch
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams
                        {
                            UserId = idsob,
                            RandomId = randid,
                            Message = crmessage,
                            Attachments = attachments
                        });
                    }
                    catch
                    {
                        try
                        {
                            api.Messages.Send(new MessagesSendParams
                            {
                                UserId = idsob,
                                RandomId = randid,
                                Message = crmessage,
                                Attachments = attachments
                            });
                        }
                        catch
                        {
                            api.Messages.Send(new MessagesSendParams
                            {
                                UserId = idsob,
                                RandomId = randid,
                                Message = crmessage,
                                Attachments = attachments
                            });
                        }
                    }
                }
            }
            else
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = idsob,
                        RandomId = randid,
                        Message = crmessage
                    });
                }
                catch
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams
                        {
                            UserId = idsob,
                            RandomId = randid,
                            Message = crmessage
                        });
                    }
                    catch
                    {
                        try
                        {
                            api.Messages.Send(new MessagesSendParams
                            {
                                UserId = idsob,
                                RandomId = randid,
                                Message = crmessage
                            });
                        }
                        catch
                        {
                            api.Messages.Send(new MessagesSendParams
                            {
                                UserId = idsob,
                                RandomId = randid,
                                Message = crmessage
                            });
                        }
                    }
                }
            }
        }

        private void SendMes(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string message = new TextRange(MyMes.Document.ContentStart, MyMes.Document.ContentEnd).Text;
                List<Image> images = GetImages(Images);
                string stringimages = "";
                foreach (Image image in images)
                {
                    try
                    {
                        stringimages += ("<Image>" + Convert.ToBase64String(ImageToByte(image.Source as BitmapImage)) + "</Image>");
                    }
                    catch
                    {
                        stringimages += ("<Image>" + Convert.ToBase64String(ImageToByte(image.Source as TransformedBitmap)) + "</Image>");
                        //MessageBox.Show(ImageToByte(image.Source as TransformedBitmap).Length.ToString());
                    }

                }
                message = Regex.Replace(message, @"\t|\n|\r", "");
                string messtr = message;
                if (stringimages != "<Image></Image>")
                {
                    message += stringimages;
                }
                Send_Message(MainWindow.api, idsob, message);
                Paragraph fullmessage = new Paragraph();
                Bold name = new Bold(new Run(MainWindow.myname + '\n'))
                {
                    Foreground = Brushes.Blue
                };
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    MyMes.Document.Blocks.Clear();
                    MyMes.Margin = new Thickness(0, 71, 10, 10);
                    Images.Document.Blocks.Clear();
                    Images.Margin = new Thickness(160, -8, 10, 10);
                }));
                fullmessage.Inlines.Add(name);
                fullmessage.Inlines.Add(messtr);
                if (images.Count > 0)
                {
                    fullmessage.Inlines.Add("\n");
                    fullmessage.Inlines.Add("\n");
                }
                foreach (Image image in images)
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        BitmapImage bitmap = image.Source as BitmapImage;
                        TransformedBitmap transformed = new TransformedBitmap();
                        if (bitmap == null)
                        {
                            transformed = new TransformedBitmap(image.Source as TransformedBitmap, new ScaleTransform(1, 1));
                            if (transformed.Height <= 1000 && transformed.Height >= 250 && transformed.Width <= 1000 && transformed.Width >= 250)
                            {
                                transformed = new TransformedBitmap(image.Source as TransformedBitmap, new ScaleTransform(0.6, 0.6));
                            }
                            else
                            {
                                if (transformed.Height <= 2000 && transformed.Width <= 2000)
                                {
                                    transformed = new TransformedBitmap(image.Source as TransformedBitmap, new ScaleTransform(0.3, 0.3));
                                }
                            }
                            image.Source = transformed;
                            image.Height = transformed.Height;
                            image.Width = transformed.Width;
                            fullmessage.Inlines.Add(image);
                            fullmessage.Inlines.Add("\n");
                            fullmessage.Inlines.Add("\n");
                            Chat.CaretPosition = Chat.Document.ContentEnd;
                            Chat.ScrollToEnd();
                        }
                        else
                        {
                            image.Height = 360.0 / 1.5;
                            image.Width = 720.0 / 1.5;
                            fullmessage.Inlines.Add(image);
                            fullmessage.Inlines.Add("\n");
                            fullmessage.Inlines.Add("\n");
                            Chat.CaretPosition = Chat.Document.ContentEnd;
                            Chat.ScrollToEnd();
                        }

                    }));
                }
                Chat.Document.Blocks.Add(fullmessage);
                Chat.Focus();
                Chat.CaretPosition = Chat.Document.ContentEnd;
                Chat.ScrollToEnd();
                MyMes.Focus();
                MyMes.Document.Blocks.Clear();
                MyMes.ScrollToHome();
            }
        }

        [STAThread]
        private void Get_Mes(object mesargums)
        {
            string sobname = MainWindow.api.Users.Get(new long[] { idsob }).FirstOrDefault().FirstName;
            string predmessage = "zhзущшепгтзкищшекгвьезипщывьгпизшщкеигекзипщцнзуищкшецкещицшугеихцущзpweouetvpowiertupmesotrmuser[topetr[vpeto,ivwe[opybiemr[po";
            Array mesargar = new object[3];
            mesargar = (Array)mesargums;
            VkApi get = (VkApi)mesargar.GetValue(0);
            int userid = (int)mesargar.GetValue(1);
            string SimKey = (string)mesargar.GetValue(2);
            string pubkey = (string)mesargar.GetValue(3);
            string privkey = (string)mesargar.GetValue(4);

            bool messtate = false;
            while (true)
            {
                string curmessage = "";
                MessagesGetObject getDialogs;
                try
                {
#pragma warning disable CS0618 // Тип или член устарел
                    getDialogs = get.Messages.GetDialogs(new MessagesDialogsGetParams
                    {
                        Count = 200
                    });
#pragma warning restore CS0618
                }
                catch
                {
#pragma warning disable CS0618
                    getDialogs = get.Messages.GetDialogs(new MessagesDialogsGetParams
                    {
                        Count = 200
                    });
#pragma warning restore CS0618 
                }

                int pos;
                for (pos = 0; pos < 200; pos++)
                {
                    if (getDialogs.Messages[pos].UserId == userid)
                    {
                        curmessage = getDialogs.Messages[pos].Body;
                        messtate = (bool)getDialogs.Messages[pos].Out;
                        break;
                    }
                }

                string decmessage = curmessage;

                try
                {
                    decmessage = Decryption(curmessage, SimKey);
                }
                catch
                {
                    decmessage = curmessage;
                }

                if (predmessage != decmessage && !messtate)
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Image img = new Image();
                        string dectext = "";
                        Regex regnt = new Regex("<Image>");
                        Regex regkt = new Regex("</Image>");
                        try
                        {
                            Attachment documentAttachment = getDialogs.Messages[pos].Attachments.First(x => x.Type == typeof(Document));
                            string uri = ((Document)documentAttachment.Instance).Uri;
                            using (WebClient webClient = new WebClient())
                            {
                                webClient.DownloadFile(uri, (Path.Combine(Environment.CurrentDirectory, "Dec_Images.txt")));
                            }
                            string enctext = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Dec_Images.txt"));
                            dectext = Decryption(enctext, SimKey);
                        }
                        catch { }
                        if (regnt.Matches(dectext).Count == 1)
                        {
                            try
                            {
                                dectext = dectext.Substring(regnt.Match(dectext).Index + regnt.Match(dectext).Length);
                                //MessageBox.Show(dectext.Length.ToString());
                                BitmapImage bitmap = ToImage(StringToByte(dectext));
                                img.Source = bitmap;
                                TransformedBitmap transformed = new TransformedBitmap();
                                if (bitmap.Height <= 1000 && bitmap.Height >= 250 && bitmap.Width <= 1000 && bitmap.Width >= 250)
                                {
                                    transformed = new TransformedBitmap(bitmap, new ScaleTransform(0.6, 0.6));
                                }
                                else
                                {
                                    if (bitmap.Height <= 2000 && bitmap.Width <= 2000)
                                    {
                                        transformed = new TransformedBitmap(bitmap, new ScaleTransform(0.3, 0.3));
                                    }
                                }
                                img.Source = transformed;
                                img.Stretch = Stretch.None;
                                img.Height = transformed.Height;
                                img.Width = transformed.Width;
                                Paragraph fullmessage = new Paragraph();
                                Bold name = new Bold(new Run(MainWindow.myname + '\n'))
                                {
                                    Foreground = Brushes.Red
                                };
                                fullmessage.Inlines.Add(name);
                                fullmessage.Inlines.Add(Regex.Split(decmessage, "<VkMKDateMes>")[0]);
                                Chat.Document.Blocks.Add(fullmessage);
                                if (dectext != "")
                                {
                                    fullmessage.Inlines.Add(Environment.NewLine);
                                    fullmessage.Inlines.Add(Environment.NewLine);
                                    fullmessage.Inlines.Add(img);
                                    fullmessage.Inlines.Add(Environment.NewLine);
                                }
                                Chat.Focus();
                                Chat.CaretPosition = Chat.Document.ContentEnd;
                                Chat.ScrollToEnd();
                                MyMes.Focus();
                                //MessageBox.Show(img.Parent.ToString());
                                //MessageBox.Show("image!!!");
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(e.ToString());
                            }
                        }
                        else
                        {
                            int pregkt = 0;
                            Paragraph fullmessage = new Paragraph();
                            Bold name = new Bold(new Run(MainWindow.myname + '\n'))
                            {
                                Foreground = Brushes.Red
                            };
                            fullmessage.Inlines.Add(name);
                            fullmessage.Inlines.Add(Regex.Split(decmessage, "<VkMKDateMes>")[0]);
                            Chat.Document.Blocks.Add(fullmessage);
                            foreach (Match match in regnt.Matches(dectext))
                            {
                                string helptext = dectext.Substring(match.Index + match.Length, regnt.Matches(dectext)[pregkt].Index-match.Index);
                                //MessageBox.Show(dectext.Length.ToString());
                                dectext.Substring(match.Index + match.Length);
                                BitmapImage bitmap = ToImage(StringToByte(helptext));
                                img.Source = bitmap;
                                TransformedBitmap transformed = new TransformedBitmap();
                                if (bitmap.Height <= 1000 && bitmap.Height >= 250 && bitmap.Width <= 1000 && bitmap.Width >= 250)
                                {
                                    transformed = new TransformedBitmap(bitmap, new ScaleTransform(0.6, 0.6));
                                }
                                else
                                {
                                    if (bitmap.Height <= 2000 && bitmap.Width <= 2000)
                                    {
                                        transformed = new TransformedBitmap(bitmap, new ScaleTransform(0.3, 0.3));
                                    }
                                }
                                img.Source = transformed;
                                img.Stretch = Stretch.None;
                                img.Height = transformed.Height;
                                img.Width = transformed.Width;
                                fullmessage.Inlines.Add(Environment.NewLine);
                                fullmessage.Inlines.Add(Environment.NewLine);
                                fullmessage.Inlines.Add(img);
                                pregkt++;
                            }
                            fullmessage.Inlines.Add(Environment.NewLine);
                            Chat.Focus();
                            Chat.CaretPosition = Chat.Document.ContentEnd;
                            Chat.ScrollToEnd();
                            MyMes.Focus();
                            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "dectestImages.txt"), dectext);
                        }
                       
                    }));
                }
                predmessage = decmessage;
                Thread.Sleep(50);
            }
        }

        private static string Decryption(string curmessage, string key)
        {
            var decryptor = new CamelliaEngine();

            var strkey = key;
            ICipherParameters param = new KeyParameter(Convert.FromBase64String(strkey));
            decryptor.Init(false, param);

            byte[] nbts = Convert.FromBase64String(curmessage);
            var ndbts = new byte[nbts.Length];
            if (ndbts.Length <= 16)
            {
                decryptor.ProcessBlock(nbts, 0, ndbts, 0);
                return Encoding.UTF8.GetString(ndbts);
            }

            for (int i = 0; i < ndbts.Length; i += 16)
            {
                decryptor.ProcessBlock(nbts, i, ndbts, i);
            }

            return Encoding.UTF8.GetString(ndbts);
        }

        private static string Encryption(string data, string key)
        {
            var encryptor = new CamelliaEngine();
            var strkey = key;
            ICipherParameters param = new KeyParameter(Convert.FromBase64String(strkey));
            encryptor.Init(true, param);
            var strlengthbytes = Encoding.UTF8.GetByteCount(data);

            if (strlengthbytes > 16 && strlengthbytes % 16 != 0)
            {
                for (int i = 0; i < 16 - strlengthbytes % 16; i++)
                {
                    data += " ";
                }

            }

            var encdata = Encoding.UTF8.GetBytes(data);

            var decmes = "";
            if (encdata.Length < 16)
            {
                for (int i = 0; i < 16 - encdata.Length; i++)
                {
                    data += " ";
                }

                encdata = Encoding.UTF8.GetBytes(data);
                byte[] decdata = new byte[encdata.Length];
                encryptor.ProcessBlock(encdata, 0, decdata, 0);
                decmes = Convert.ToBase64String(decdata);
            }


            if (encdata.Length > 16)
            {

                byte[] decdata = new byte[encdata.Length];
                for (int i = 0; i < encdata.Length; i += 16)
                {
                    encryptor.ProcessBlock(encdata, i, decdata, i);
                    if (i + 16 > encdata.Length)
                    {
                        break;
                    }
                }

                decmes = Convert.ToBase64String(decdata);
            }

            return decmes;
        }

        private string ChangeKeys(VkApi api, string pubkey, int idsob, ref bool me_or_him)
        {
            string newpubkey;
            if (Check_Key(api, idsob) == false)
            {
                Send_Key(api, pubkey, idsob, true);
                newpubkey = Get_Key(api, idsob);
                me_or_him = true;
            }
            else
            {
                me_or_him = false;
                newpubkey = Get_Key(api, idsob);
                Send_Key(api, pubkey, idsob, false);
            }

            return newpubkey;
        }

        private bool Check_Key(VkApi api, int idsob)
        {
            bool pr = true;

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Идет проверка на присутствие ключа в чате...")));
            }));

#pragma warning disable CS0618 // Тип или член устарел
            var getDialogs = api.Messages.GetDialogs(new MessagesDialogsGetParams
            {
                Count = 200
            });
#pragma warning restore CS0618 // Тип или член устарел
            var curmessage = "";
            var state = false;
            for (var i = 0; i < 200; i++)
            {
                if (getDialogs.Messages[i].UserId == idsob)
                {
                    state = (bool)getDialogs.Messages[i].Out;
                    curmessage = getDialogs.Messages[i].Body;
                    break;
                }
            }

            if (curmessage.Length < 13)
            {
                pr = false;
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Собеседник не отправил Вам свой ключ((")));
                }));

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Опять вся надежда на Вас!!!")));
                }));

                return false;
            }

            if (curmessage.Substring(0, 13) == "<RSAKeyValue>" && state == false)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ есть!" + Environment.NewLine)));
                }));

                return true;
            }

            if (pr)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Собеседник не отправил Вам свой ключ((")));
                }));

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Опять вся надежда на Вас!!!")));
                }));

                return false;
            }

            return false;
        }

        private bool Check_Key_Without_GUI(VkApi api, int idsob)
        {
            bool pr = true;

#pragma warning disable CS0618 // Тип или член устарел
            MessagesGetObject getDialogs = api.Messages.GetDialogs(new MessagesDialogsGetParams
            {
                Count = 200
            });
#pragma warning restore CS0618 // Тип или член устарел
            var curmessage = "";
            var state = false;
            for (var i = 0; i < 200; i++)
            {
                if (getDialogs.Messages[i].UserId == idsob)
                {
                    state = (bool)getDialogs.Messages[i].Out;
                    curmessage = getDialogs.Messages[i].Body;
                    break;
                }
            }

            if (curmessage.Length < 13)
            {
                return false;
            }

            if (curmessage.Substring(0, 13) == "<RSAKeyValue>" && state == false)
            {
                return true;
            }

            if (pr)
            {
                return false;
            }

            return false;
        }

        private void Send_Key(VkApi api, string pubkey, int idsob, bool pr)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Идет отправка публичного ключа...")));
            }));
            var random = new Random();
            var randid = random.Next(99999);
            try
            {
                api.Messages.Send(new MessagesSendParams
                {
                    UserId = idsob,
                    RandomId = randid,
                    Message = pubkey
                });
            }
            catch
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = idsob,
                        RandomId = randid,
                        Message = pubkey
                    });
                }
                catch (Exception e)
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Chat.Document.Blocks.Add(new Paragraph(new Run(e.ToString())));
                    }));
                }
            }
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ успешно отправлен!!!")));
            }));
            if (pr)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Дожидаемся получения ключа...")));
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Можете сходить за кофе")));
                }));
            }
        }

        private string Get_Key(VkApi api, int idsob)
        {
            string newkey;
            while (true)
            {
#pragma warning disable CS0618 // Тип или член устарел
                MessagesGetObject getDialogs = api.Messages.GetDialogs(new MessagesDialogsGetParams
                {
                    Count = 200
                });
#pragma warning restore CS0618 // Тип или член устарел
                Thread.Sleep(500);
                var curmessage = "";
                var state = false;
                int i;
                for (i = 0; i < 200; i++)
                {
                    if (getDialogs.Messages[i].UserId == idsob)
                    {
                        state = (bool)getDialogs.Messages[i].Out;
                        curmessage = getDialogs.Messages[i].Body;
                        break;
                    }
                }

                if (curmessage.Length < 13)
                {
                    continue;
                }

                if (curmessage.Substring(0, 13) == "<RSAKeyValue>" && state == false)
                {
                    newkey = curmessage;
                    break;
                }
            }

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ получен")));
            }));
            return newkey;
        }

        public void Content_Inputed(object sender, TextChangedEventArgs e)
        {
            ResizeRtbImages(sender as RichTextBox);
        }

        private void ResizeRtbImages(RichTextBox rtb)
        {
            foreach (Block block in rtb.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    Paragraph paragraph = (Paragraph)block;
                    foreach (Inline inline in paragraph.Inlines)
                    {
                        if (inline is InlineUIContainer)
                        {
                            InlineUIContainer uiContainer = (InlineUIContainer)inline;
                            if (uiContainer.Child is Image)
                            {
                                Image img = (Image)uiContainer.Child;
                                img.Height = 75;
                                img.Width = 150;
                                if (Images.Margin == new Thickness(160, -8, 10, 10))
                                {
                                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                                    {
                                        MyMes.Margin = new Thickness(0, 71, 150, 10);
                                        Images.Margin = new Thickness(5, -5, 10, 10);
                                    }));
                                }
                                Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    uiContainer.Child = null;
                                    Images.Document.Blocks.Add(new BlockUIContainer(img));
                                    Images.Focus();
                                    Images.CaretPosition = Images.Document.ContentEnd;
                                    Images.ScrollToEnd();
                                    MyMes.Focus();
                                }));
                            }
                        }
                    }
                }
                if (block is BlockUIContainer)
                {
                    BlockUIContainer blockui = (BlockUIContainer)block;
                    if (blockui.Child is Image)
                    {
                        Image img = (Image)blockui.Child;
                        img.Height = 75;
                        img.Width = 150;
                        if (Images.Margin == new Thickness(160, -8, 10, 10))
                        {
                            Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                MyMes.Margin = new Thickness(0, 71, 150, 10);
                                Images.Margin = new Thickness(5, -5, 10, 10);
                            }));
                        }
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            blockui.Child = null;
                            Images.Document.Blocks.Add(new BlockUIContainer(img));
                            Images.Focus();
                            Images.CaretPosition = Images.Document.ContentEnd;
                            Images.ScrollToEnd();
                            MyMes.Focus();
                        }));
                    }
                }
            }
        }

        private List<Image> GetImages(RichTextBox rtb)
        {
            List<Image> images = new List<Image>();
            foreach (Block block in rtb.Document.Blocks)
            {
                if (block is BlockUIContainer)
                {
                    BlockUIContainer blockui = (BlockUIContainer)block;
                    if (blockui.Child is Image)
                    {
                        Image img = (Image)blockui.Child;
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            blockui.Child = null;
                        }));
                        images.Add(img);
                    }
                }
            }
            return images;
        }

        public static byte[] ImageToByte(BitmapImage image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        public static byte[] ImageToByte(TransformedBitmap image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        private void ImageEntered(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0 && files.Where(IsImageFile).Any())
            {
                e.Handled = true;
            }
        }

        private void ImageDropped(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            //MessageBox.Show(files[0]);
            if (Path.GetExtension(files[0]) == ".jpg" || Path.GetExtension(files[0]) == ".jpeg" || Path.GetExtension(files[0]) == ".png")
            {
                BitmapImage bitmap = new BitmapImage(new Uri(files[0]));
                TransformedBitmap targetBitmap = new TransformedBitmap(bitmap, new ScaleTransform(1, 1));
                Image img = new Image();
                img.Source = targetBitmap;
                img.Stretch = Stretch.None;
                img.Height = 75;
                img.Width = 150;
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    MyMes.Margin = new Thickness(0, 71, 150, 10);
                    Images.Margin = new Thickness(5, -5, 10, 10);
                }));
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Images.Document.Blocks.Add(new BlockUIContainer(img));
                    Images.Focus();
                    Images.CaretPosition = Images.Document.ContentEnd;
                    Images.ScrollToEnd();
                    MyMes.Focus();
                }));
            }
        }

        private static bool IsImageFile(string fileName)
        {
            return true;
        }

        public BitmapImage ToImage(byte[] array)
        {
            using (var ms = new MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad; // here
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        public static byte[] StringToByte(string s)
        {
            byte[] encbytes = new byte[0];
            while (s.Length > 52000)
            {
                encbytes = Combine(encbytes, Convert.FromBase64String(s.Substring(0,10000)));
                s = s.Substring(10000);
            }
            encbytes = Combine(encbytes,Convert.FromBase64String(s));
            return encbytes;
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}
