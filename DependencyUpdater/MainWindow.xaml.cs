using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace DependencyUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string feed = "";
        private string _folderPath;
        public MainWindow()
        {
            InitializeComponent();
        }

        public string FolderPath
        {
            get => _folderPath;
            set
            {
                _folderPath = value;
                OnPropertyChanged("FolderPath");
                
            }
        }

        public string Feed { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Get_version_button_Click(object sender, RoutedEventArgs e)
        {
            feed = Enter_required_nuget_feed_version_field.Text;
        }

        private void Enter_required_nuget_feed_version_field_TextChanged(object sender, TextChangedEventArgs e)
        {
            Feed = Enter_required_nuget_feed_version_field.Text;
        }

        private void Browse_button_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                
                DialogResult result = dlg.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    FolderPath = dlg.SelectedPath;
                }
            }
        }

        private void Path_to_code_folder_field_TextChanged(object sender, TextChangedEventArgs e)
        {
            Path_to_code_folder_field.Text = FolderPath;
        }
    }
}
