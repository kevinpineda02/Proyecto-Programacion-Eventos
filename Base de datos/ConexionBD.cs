using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace GestorRRHH.Base_de_datos
{
    // Clase encargada de gestionar la comunicación con el servidor de base de datos SQL Server.
    // Centraliza la cadena de conexión para que cualquier cambio se realice en un solo lugar.
    public class ConexionBD
    {
        // Cadena de conexión que contiene los parámetros necesarios para acceder a la base de datos.
        // Incluye el nombre del servidor, el nombre de la base de datos y la configuración de seguridad.
        private static readonly string cadenaConexion = "Data Source=KEVINPINEDA;Initial Catalog=GestorRRHH;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

        // Método estático que intenta establecer y abrir una conexión con la base de datos.
        // Devuelve un objeto SqlConnection abierto si tiene éxito, o null si ocurre un fallo.
        public static SqlConnection ObtenerConexion()
        {
            // Se crea una nueva instancia del objeto de conexión usando la cadena definida anteriormente.
            SqlConnection conexion = new SqlConnection(cadenaConexion);
            try
            {
                // Intenta abrir la conexión con el servidor.
                // Si el servidor no responde o los datos son incorrectos, esto generará una excepción.
                conexion.Open();
                return conexion;
            }
            catch (Exception ex)
            {
                // Bloque de captura de errores. Si la conexión falla, se notifica al usuario.
                // Se muestra el mensaje técnico de la excepción para facilitar la depuración.
                MessageBox.Show("Error al conectar a la base de datos: " + ex.Message, "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Retorna nulo para indicar a quien llamó el método que la conexión no fue posible.
                return null;
            }
        }
    }
}