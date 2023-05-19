using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private const string AccountDataFileName = "accountData.dat";



        private ObservableCollection<Account> accounts;
        public ObservableCollection<Account> Accounts
        {
            get { return accounts; }
            set
            {
                accounts = value;
                OnPropertyChanged(nameof(Accounts));
            }
        }

        private Account selectedAccount;
        public Account SelectedAccount
        {
            get { return selectedAccount; }
            set
            {
                selectedAccount = value;
                OnPropertyChanged(nameof(SelectedAccount));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Accounts = new ObservableCollection<Account>();
            //LoadAccountData();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /*private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveAccountData();
        }*/

        /*private void SaveAccountData()
        {
            using (FileStream fs = new FileStream(AccountDataFileName, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, Accounts);
            }
        }*/

        /*private void LoadAccountData()
        {
            if (File.Exists(AccountDataFileName))
            {
                using (FileStream fs = new FileStream(AccountDataFileName, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    Accounts = (ObservableCollection<Account>)formatter.Deserialize(fs);
                }
            }
            else
            {
                Accounts = new ObservableCollection<Account>();
            }
        }*/

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            string gmail = txtGmail.Text;
            string username = txtUsername.Text;
            string password = txtPassword.Password;

            Account newAccount = new Account(gmail, username, password);
            Accounts.Add(newAccount);

            // Clear the input fields
            txtGmail.Clear();
            txtUsername.Clear();
            txtPassword.Clear();

        }

        private void RemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAccount != null)
            {
                Accounts.Remove(SelectedAccount);
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt";
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] accountData = line.Split(',');
                        if (accountData.Length == 3)
                        {
                            string decryptedGmail = Decrypt(accountData[0]);
                            string decryptedUsername = Decrypt(accountData[1]);
                            string decryptedPassword = Decrypt(accountData[2]);

                            Account account = new Account(decryptedGmail, decryptedUsername, decryptedPassword);
                            Accounts.Add(account);
                        }
                    }
                }

                MessageBox.Show("Accounts imported successfully!");
            }
        }

        private string Encrypt(string data)
        {
            byte[] key;
            byte[] encryptedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                key = aes.Key;

                aes.IV = new byte[16];

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(dataBytes, 0, dataBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            string encryptedData = Convert.ToBase64String(encryptedBytes);

            // Prepend the encryption key to the encrypted data (for decryption)
            encryptedData = Convert.ToBase64String(key) + "|" + encryptedData;

            return encryptedData;
        }

        private string Decrypt(string data)
        {
            string[] parts = data.Split('|');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid encrypted data format.");
            }

            byte[] key = Convert.FromBase64String(parts[0]);
            byte[] encryptedBytes = Convert.FromBase64String(parts[1]);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = new byte[16];

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        byte[] decryptedBytes = new byte[encryptedBytes.Length];
                        int decryptedByteCount = cs.Read(decryptedBytes, 0, decryptedBytes.Length);

                        return Encoding.UTF8.GetString(decryptedBytes, 0, decryptedByteCount);
                    }
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    foreach (Account account in Accounts)
                    {
                        string encryptedGmail = Encrypt(account.Gmail);
                        string encryptedUsername = Encrypt(account.Username);
                        string encryptedPassword = Encrypt(account.Password);

                        writer.WriteLine(encryptedGmail + "," + encryptedUsername + "," + encryptedPassword);
                    }
                }

                MessageBox.Show("Accounts exported successfully!");
            }
        }

        private void ChkField_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            string checkBoxName = checkBox.Name;

            switch (checkBoxName)
            {
                case "chkGmail":
                    txtGmail.IsEnabled = true;
                    break;
                case "chkUsername":
                    txtUsername.IsEnabled = true;
                    break;
                case "chkPassword":
                    txtPassword.IsEnabled = true;
                    break;
            }
        }

        private void ChkField_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            string checkBoxName = checkBox.Name;

            switch (checkBoxName)
            {
                case "chkGmail":
                    txtGmail.IsEnabled = false;
                    break;
                case "chkUsername":
                    txtUsername.IsEnabled = false;
                    break;
                case "chkPassword":
                    txtPassword.IsEnabled = false;
                    break;
            }
        }
    }

    public partial class MainWindow : Window
    {
        public MainWindow(string projectName)
        {
            InitializeComponent();
            Title = $"{projectName} - Password Manager";
        }

        // Rest of the code...
    }


    public class Account
    {
        public string Gmail { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public Account(string gmail, string username, string password)
        {
            Gmail = gmail;
            Username = username;
            Password = password;
        }
    }
}
