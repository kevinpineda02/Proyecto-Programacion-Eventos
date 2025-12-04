using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using GestorRRHH.Base_de_datos;
using System.Text.RegularExpressions; // Librería para validaciones de patrones de texto

namespace GestorRRHH
{
    /// <summary>
    /// Formulario de registro de nuevos usuarios en el sistema GestorRRHH. 
    /// Permite crear cuentas con validaciones de seguridad avanzadas para contraseñas.
    /// </summary>
    public partial class Registro : Form
    {
        /// <summary>
        /// Constructor del formulario Registro.
        /// Inicializa los componentes de la interfaz gráfica.
        /// </summary>
        public Registro()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Evento del botón "Registrar" que maneja el proceso de creación de nuevos usuarios.
        /// Incluye validaciones de seguridad para contraseñas y manejo de duplicados.
        /// </summary>
        /// <param name="sender">Objeto que disparó el evento</param>
        /// <param name="e">Argumentos del evento</param>
        private void btnRegistrar_Click(object sender, EventArgs e)
        {
            // Obtener la contraseña ingresada para las validaciones
            string pass = txtPasswordNuevo.Text;

            // Validar que todos los campos requeridos estén completos
            if (string.IsNullOrWhiteSpace(txtUsuarioNuevo.Text) ||
                string.IsNullOrWhiteSpace(pass) ||
                string.IsNullOrWhiteSpace(txtConfirmarPass.Text))
            {
                MessageBox.Show("Por favor completa todos los campos.", "Campos Incompletos", 
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validaciones de seguridad para la contraseña

            // Validación A: Verificar longitud mínima de 14 caracteres
            if (pass.Length < 14)
            {
                MessageBox. Show("La contraseña es muy débil.\nDebe tener al menos 14 caracteres.", 
                              "Seguridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validación B: Verificar presencia de caracteres especiales
            // Expresión regular que busca cualquier carácter que no sea letra ni número
            Regex tieneEspeciales = new Regex(@"[^a-zA-Z0-9]");

            if (! tieneEspeciales.IsMatch(pass))
            {
                MessageBox.Show("La contraseña debe incluir al menos un carácter especial.\nEjemplo: @, #, $, %, &", 
                              "Seguridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar que ambas contraseñas ingresadas coincidan
            if (pass != txtConfirmarPass. Text)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Error", 
                              MessageBoxButtons. OK, MessageBoxIcon.Error);
                return;
            }

            // Proceso de inserción en la base de datos SQL Server
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                // Verificar que la conexión a la base de datos sea exitosa
                if (con == null) return;

                try
                {
                    // Consulta SQL para insertar nuevo usuario con rol de Empleado por defecto
                    string query = "INSERT INTO Usuarios (Login, Password, Rol) VALUES (@login, @pass, 'Empleado')";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Agregar parámetros para prevenir inyección SQL
                        cmd.Parameters.AddWithValue("@login", txtUsuarioNuevo.Text. Trim());
                        cmd.Parameters.AddWithValue("@pass", pass.Trim()); // Contraseña ya validada

                        // Ejecutar la consulta de inserción
                        int filas = cmd.ExecuteNonQuery();

                        if (filas > 0)
                        {
                            // Registro exitoso: mostrar mensaje y redirigir al login
                            MessageBox.Show("¡Cuenta creada con éxito!", "Registro Completado", 
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Abrir formulario de login y cerrar el de registro
                            Login login = new Login();
                            login.Show();
                            this.Hide();
                        }
                    }
                }
                catch (SqlException ex)
                {
                    // Manejo específico de errores SQL
                    if (ex.Number == 2627 || ex.Number == 2601) // Códigos de error para duplicados
                    {
                        MessageBox.Show("Ese usuario ya está registrado.\nIntenta con otro correo.", 
                                      "Usuario Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        // Otros errores de base de datos
                        MessageBox.Show("Error de base de datos: " + ex.Message, "Error", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores generales no relacionados con SQL
                    MessageBox.Show("Error general: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Evento del botón cerrar aplicación.
        /// Termina completamente la ejecución del programa.
        /// </summary>
        /// <param name="sender">Objeto que disparó el evento</param>
        /// <param name="e">Argumentos del evento</param>
        private void buttonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Evento del link "Ya tengo cuenta" que redirige al formulario de login.
        /// Primer método de navegación al login.
        /// </summary>
        /// <param name="sender">Objeto que disparó el evento</param>
        /// <param name="e">Argumentos del evento LinkLabel</param>
        private void labelCuenta_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Abrir formulario de login y ocultar registro
            Login login = new Login();
            login.Show();
            this.Hide();
        }

        /// <summary>
        /// Evento alternativo del link "Ya tengo cuenta" que redirige al formulario de login.
        /// Segundo método de navegación al login (método duplicado).
        /// </summary>
        /// <param name="sender">Objeto que disparó el evento</param>
        /// <param name="e">Argumentos del evento LinkLabel</param>
        private void labelCuenta_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Abrir formulario de login y ocultar registro
            Login login = new Login();
            login.Show();
            this.Hide();
        }
    }
} 