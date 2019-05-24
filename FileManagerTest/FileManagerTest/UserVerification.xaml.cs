﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FileManagerTest.Model;
using Dapper;
using System.Security.Cryptography;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace FileManagerTest.View
{
    /// <summary>
    /// Interaction logic for UserVerification.xaml
    /// </summary>
    public partial class UserVerification : Window
    {

        public UserVerification()
        {
            InitializeComponent();
        }
        public static IDbConnection DapperConnection()
        {
            IDbConnection _db = new SqlConnection(ConfigurationManager.ConnectionStrings["conString"].ConnectionString);
            return _db;
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if(ValidateUser(txtUsername.Text, txtPassword.Password))
            {
                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Username or password is incorrect");
            }
        }

        public static bool ValidateUser(string username, string password)
        {
            bool isValidUser = false;
            string _Password = string.Empty;

            try
            {
                _Password = EncodePassword(password);
                System.Data.IDbConnection _db = DapperConnection();
                List<userListModel> trxs = _db.Query<userListModel>(";Exec AuthenticateUser @UserName, @Password",
                    new
                    {
                        UserName = username,
                        Password = _Password
                    }).ToList();

                //List<User> trxs = con.Fetch<User>(";Exec getAuthenticateUser @UserName, @Password", new { UserName= username, Password= _Password });

                isValidUser = trxs[0].UserName.ToString().ToUpper().Trim().Equals(username.ToUpper());
            }
            catch (Exception ee)
            {

            }

            return isValidUser;
        }

        public static string EncodePassword(string value)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(value));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

    }
}
