using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Mapcapture
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {

            
            MainWindow MainWindow = new MainWindow(e);
            MainWindow.Title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name.ToString() + " " +  System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            if (e.Args.Length > 0) {

                // MessageBox.Show("Now opening file: \n\n" + e.Args[0]);

            }

            MainWindow.Show();

            
        }
    }
}
