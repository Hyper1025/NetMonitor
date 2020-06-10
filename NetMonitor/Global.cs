using System.Threading.Tasks;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace NetMonitor
{
    public class Global
    {
        internal static Snackbar BarraNotifica { get; set; }

        public static void Notificar(string texto, string corHex = null)
        {
            if (corHex == null)
            {
                corHex = "#4ad295";
            }

            BarraNotifica.Background = HexToColorBrushConverter(corHex);

            var filaMsg = BarraNotifica.MessageQueue;
            Task.Factory.StartNew(() => filaMsg.Enqueue(texto));
        }

        public static SolidColorBrush HexToColorBrushConverter(string hex)
        {
            var cor = (SolidColorBrush)(new BrushConverter().ConvertFrom(hex));
            return cor;
        }
    }
}