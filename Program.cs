using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GestorRRHH
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // En .NET Framework, la configuración de nitidez (DPI) 

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Tu formulario de inicio
            Application.Run(new Login());
        }
    }
}