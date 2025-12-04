using System;
using System.Windows.Forms;
using System.Data.SqlClient; // Necesario para trabajar con SQL Server
using GestorRRHH.Base_de_datos; // Para usar la clase ConexionBD

namespace GestorRRHH
{
    /// <summary>
    /// Formulario de inicio de sesión del sistema GestorRRHH. 
    /// Permite a los usuarios autenticarse y acceder al sistema según su rol.
    /// </summary>
    public partial class Login : Form
    {
        /// <summary>
        /// Constructor del formulario Login.
        /// Inicializa los componentes de la interfaz gráfica.
        /// </summary>
        public Login()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Evento del botón "Ingresar" que maneja el proceso de autenticación.
        /// Valida credenciales contra la base de datos y establece la sesión del usuario.
        /// </summary>
        /// <param name="sender">Objeto que disparó el evento</param>
        /// <param name="e">Argumentos del evento</param>
        private void btnLoguin_Click(object sender, EventArgs e)
        {
            // Validar que los campos de usuario y contraseña no estén vacíos
            if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtContrasena.Text))
            {
                MessageBox.Show("Por favor ingresa usuario y contraseña.", "Campos Vacíos",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Establecer conexión con la base de datos para autenticación
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                // Verificar que la conexión se estableció correctamente
                if (con == null) return;

                try
                {
                    // Consulta SQL para buscar usuario y contraseña en la tabla Usuarios
                    // Utiliza parámetros para prevenir inyección SQL
                    string query = "SELECT IdUsuario, Login, Rol FROM Usuarios WHERE Login = @user AND Password = @pass";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Agregar parámetros a la consulta para seguridad
                        cmd.Parameters.AddWithValue("@user", txtUsuario.Text.Trim());
                        cmd.Parameters.AddWithValue("@pass", txtContrasena.Text.Trim());

                        // Ejecutar consulta y leer resultados
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Login exitoso: establecer sesión del usuario
                                SesionActual.IdUsuario = Convert.ToInt32(reader["IdUsuario"]);
                                SesionActual.NombreUsuario = reader["Login"].ToString();
                                SesionActual.Rol = reader["Rol"].ToString();

                                // Mostrar mensaje de bienvenida con información del usuario
                                MessageBox.Show($"Bienvenido, {SesionActual.NombreUsuario}.\nTu rol es: {SesionActual.Rol}",
                                                "Acceso Correcto", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // Abrir el menú principal del sistema y ocultar formulario de login
                                Crear_CV menuPrincipal = new Crear_CV(this);
                                menuPrincipal.Show();
                                this.Hide();
                            }
                            else
                            {
                                // Login fallido: mostrar mensaje de error
                                MessageBox.Show("Usuario o contraseña incorrectos.", "Error de Acceso",
                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores críticos del sistema
                    MessageBox.Show("Error crítico en el sistema: " + ex.Message, "Error",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Evento del link "Crear Cuenta" que redirige al formulario de registro.
        /// Permite a nuevos usuarios registrarse en el sistema.
        /// </summary>
        /// <param name="sender">Objeto que disparó el evento</param>
        /// <param name="e">Argumentos del evento LinkLabel</param>
        private void labelCrearCuenta_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Abrir formulario de registro y ocultar login
            Registro registro = new Registro();
            registro.Show();
            this.Hide();
        }

        /// <summary>
        /// Evento del botón cerrar aplicación. 
        /// Termina completamente la ejecución del programa.
        /// </summary>
        /// <param name="sender">Objeto que disparó el evento</param>
        /// <param name="e">Argumentos del evento</param>
        private void buttonClose_Click(object sender, EventArgs e)
        {
            // Cerrar toda la aplicación
            Application.Exit();
        }
    }
}