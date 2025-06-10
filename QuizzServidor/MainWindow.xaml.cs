using QuizzServidor.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuizzServidor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();



            //ContenidoActual.Content = new AgregarPreguntaView();

        }

        //public void CambiarVista(UserControl nuevaVista)
        //{
        //    ContenidoActual.Content = nuevaVista;
        //}
    }
}