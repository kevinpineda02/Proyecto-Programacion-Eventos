using GestorRRHH.Agregar_CV;
using GestorRRHH.Base_de_datos;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace GestorRRHH
{
    public partial class Empleado : Form
    {
        // Variable para almacenar referencia al formulario anterior
        private Form formAnterior;

        // Constructor que recibe el formulario anterior
        public Empleado(Form anterior)
        {
            InitializeComponent();
            this.formAnterior = anterior;
        }

        // Constructor por defecto
        public Empleado()
        {
            InitializeComponent();
        }

        // Evento que se ejecuta al cargar el formulario
        private void Empleado_Load_1(object sender, EventArgs e)
        {
            // Validar si el usuario actual es administrador
            if (SesionActual.Rol != "Admin")
            {
                // Ocultar controles si no tiene permisos
                dgvEmpleados.Visible = false;
                txtBuscarNombre.Visible = false;
                MessageBox.Show("Acceso denegado: Se requieren permisos de Administrador.", "Seguridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                return;
            }

            // Si es admin, cargar la lista de empleados
            CargarListaEmpleados();

            // Configurar la funcionalidad de búsqueda unificada
            ConfigurarBusquedaUnificada();
        }

        // Configura la búsqueda unificada en una sola barra de texto
        private void ConfigurarBusquedaUnificada()
        {
            if (txtBuscarNombre != null)
            {
                // Limpiar el campo de búsqueda
                txtBuscarNombre.Text = "";

                // Configurar búsqueda en tiempo real
                txtBuscarNombre.TextChanged += (s, e) => CargarListaEmpleados();

                // Configurar tooltip con instrucciones de uso
                ToolTip tooltip = new ToolTip();
                tooltip.SetToolTip(txtBuscarNombre,
                    "Búsqueda Universal\n" +
                    "• Buscar por nombre: Juan, María\n" +
                    "• Buscar por departamento: IT, Ventas\n" +
                    "• Buscar por habilidad: C#, Python\n" +
                    "• Buscar por email: @gmail.com\n" +
                    "• Buscar por teléfono: 7912");
            }
        }

        // Cargar y filtrar la lista de empleados desde la base de datos
        private void CargarListaEmpleados()
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                if (con == null) return;

                try
                {
                    // Consulta SQL unificada que busca en todos los campos relevantes
                    string query = @"
                    SELECT DISTINCT
                        U.IdUsuario, 
                        ISNULL(D. Nombre, U.Login) AS Nombre, 
                        ISNULL(D.Telefono, '---') AS Telefono, 
                        ISNULL(D.Correo, 'Sin registrar') AS Correo, 
                        ISNULL(D.Departamento, 'General') AS Departamento,
                        U.Estado,
                        ISNULL(
                            STUFF((
                                SELECT ', ' + H.Habilidad 
                                FROM Habilidades H 
                                WHERE H. IdUsuario = U.IdUsuario
                                FOR XML PATH('')
                            ), 1, 2, ''), 
                            'Sin habilidades'
                        ) AS Habilidades,
                        U.Rol
                    FROM dbo.Usuarios U
                    LEFT JOIN dbo.DatosPersonales D ON U.IdUsuario = D.IdUsuario
                    WHERE U. Rol IN ('Empleado', 'Admin')
                    AND (
                        @Busqueda = '' 
                        OR (ISNULL(D.Nombre, U.Login) LIKE '%' + @Busqueda + '%')           -- Buscar en nombre
                        OR (ISNULL(D.Departamento, 'General') LIKE '%' + @Busqueda + '%')  -- Buscar en departamento  
                        OR (ISNULL(D.Correo, '') LIKE '%' + @Busqueda + '%')               -- Buscar en correo
                        OR (ISNULL(D.Telefono, '') LIKE '%' + @Busqueda + '%')             -- Buscar en teléfono
                        OR (U.Login LIKE '%' + @Busqueda + '%')                            -- Buscar en login
                        OR (U.Rol LIKE '%' + @Busqueda + '%')                              -- Buscar en rol
                        OR EXISTS(                                                          -- Buscar en habilidades
                            SELECT 1 FROM Habilidades H2 
                            WHERE H2. IdUsuario = U.IdUsuario 
                            AND (
                                H2.Habilidad LIKE '%' + @Busqueda + '%'
                                OR H2.Competencia LIKE '%' + @Busqueda + '%'
                                OR H2.Dominio LIKE '%' + @Busqueda + '%'
                            )
                        )
                    )
                    ORDER BY U. Rol DESC, ISNULL(D.Nombre, U.Login)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Obtener el texto de búsqueda del campo unificado
                        string busquedaUnificada = txtBuscarNombre?.Text?.Trim() ?? "";
                        cmd.Parameters.AddWithValue("@Busqueda", busquedaUnificada);

                        // Ejecutar consulta y llenar DataTable
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        // Limpiar y configurar el DataGridView
                        dgvEmpleados.DataSource = null;
                        dgvEmpleados.Columns.Clear();
                        dgvEmpleados.DataSource = dt;

                        ConfigurarGrid();

                        // Actualizar título de la ventana con información de resultados
                        string tituloBase = $"Usuarios del Sistema - {dt.Rows.Count} resultado(s)";

                        if (!string.IsNullOrEmpty(busquedaUnificada))
                        {
                            tituloBase += $" | Búsqueda: '{busquedaUnificada}'";

                            // Mostrar qué tipo de búsqueda se detectó
                            string tipoBusqueda = DetectarTipoBusqueda(busquedaUnificada);
                            if (!string.IsNullOrEmpty(tipoBusqueda))
                            {
                                tituloBase += $" ({tipoBusqueda})";
                            }
                        }

                        this.Text = tituloBase;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error SQL: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Detecta automáticamente el tipo de búsqueda basándose en patrones
        private string DetectarTipoBusqueda(string busqueda)
        {
            if (string.IsNullOrEmpty(busqueda)) return "";

            busqueda = busqueda.ToLower();

            // Detectar patrones específicos
            if (busqueda.Contains("@")) return "Email";
            if (busqueda.All(char.IsDigit) && busqueda.Length > 3) return "Teléfono";
            if (busqueda.Contains("admin") || busqueda.Contains("empleado")) return "Rol";

            // Detectar departamentos comunes
            string[] departamentosComunes = { "it", "sistemas", "rrhh", "recursos", "humanos", "ventas", "contabil", "general", "admin" };
            if (departamentosComunes.Any(d => busqueda.Contains(d))) return "Departamento";

            // Detectar tecnologías y habilidades comunes
            string[] habilidadesComunes = { "c#", "java", "python", "excel", "sql", "html", "css", "javascript", "react", "angular", "php" };
            if (habilidadesComunes.Any(h => busqueda.Contains(h))) return "Habilidad";

            // Si tiene espacios, probablemente sea un nombre
            if (busqueda.Contains(" ")) return "Nombre completo";

            return "General";
        }

        // Configura la apariencia y funcionalidad del DataGridView
        private void ConfigurarGrid()
        {
            if (dgvEmpleados.DataSource == null) return;

            // Configuraciones básicas del DataGridView
            dgvEmpleados.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvEmpleados.RowHeadersVisible = false;
            dgvEmpleados.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEmpleados.AllowUserToAddRows = false;
            dgvEmpleados.ReadOnly = true;
            dgvEmpleados.BackgroundColor = Color.White;
            dgvEmpleados.BorderStyle = BorderStyle.None;

            // Ocultar la columna ID del usuario
            if (dgvEmpleados.Columns.Contains("IdUsuario"))
                dgvEmpleados.Columns["IdUsuario"].Visible = false;

            // Configurar columnas, colores y botones
            ConfigurarColumnasGrid();
            ConfigurarColoresGrid();
            AgregarBotonesGrid();
        }

        // Configura el ancho y título de las columnas
        private void ConfigurarColumnasGrid()
        {
            var configuraciones = new Dictionary<string, (int ancho, string titulo)>
            {
                { "Nombre", (150, "Nombre") },
                { "Telefono", (100, "Teléfono") },
                { "Correo", (180, "Correo") },
                { "Departamento", (120, "Departamento") },
                { "Estado", (80, "Estado") },
                { "Rol", (80, "Tipo") },
                { "Habilidades", (200, "Habilidades") }
            };

            foreach (var config in configuraciones)
            {
                if (dgvEmpleados.Columns.Contains(config.Key))
                {
                    dgvEmpleados.Columns[config.Key].Width = config.Value.ancho;
                    dgvEmpleados.Columns[config.Key].HeaderText = config.Value.titulo;

                    if (config.Key == "Habilidades")
                    {
                        dgvEmpleados.Columns[config.Key].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                        dgvEmpleados.Columns[config.Key].DefaultCellStyle.Font = new Font("Arial", 8);
                    }
                }
            }
        }

        // Configura los colores de las filas según estado y rol
        private void ConfigurarColoresGrid()
        {
            foreach (DataGridViewRow row in dgvEmpleados.Rows)
            {
                string estado = row.Cells["Estado"].Value?.ToString() ?? "";
                string rol = row.Cells["Rol"].Value?.ToString() ?? "";

                if (estado == "Inactivo")
                {
                    // Usuarios inactivos en gris
                    row.DefaultCellStyle.BackColor = Color.LightGray;
                    row.DefaultCellStyle.ForeColor = Color.DarkGray;
                }
                else if (rol == "Admin")
                {
                    // Destacar administradores con color especial
                    row.DefaultCellStyle.BackColor = Color.LightCyan;
                    row.DefaultCellStyle.ForeColor = Color.DarkBlue;
                    row.DefaultCellStyle.Font = new Font(dgvEmpleados.Font, FontStyle.Bold);
                }
                else
                {
                    // Empleados normales en blanco
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
            }
        }

        // Agrega los botones de acción al DataGridView
        private void AgregarBotonesGrid()
        {
            // Botón Ver CV
            if (!dgvEmpleados.Columns.Contains("VerCV"))
            {
                DataGridViewButtonColumn btnEdit = new DataGridViewButtonColumn
                {
                    Name = "VerCV",
                    HeaderText = "Acción",
                    Text = "Ver CV", // ÚNICO EMOJI MANTENIDO
                    UseColumnTextForButtonValue = true,
                    Width = 80
                };
                btnEdit.DefaultCellStyle.BackColor = Color.LightBlue;
                dgvEmpleados.Columns.Add(btnEdit);
            }

            // Botón Cambiar Estado
            if (!dgvEmpleados.Columns.Contains("Eliminar"))
            {
                DataGridViewButtonColumn btnDel = new DataGridViewButtonColumn
                {
                    Name = "Eliminar",
                    HeaderText = "Estado",
                    Text = "Cambiar Estado",
                    UseColumnTextForButtonValue = true,
                    Width = 60
                };
                btnDel.DefaultCellStyle.ForeColor = Color.Red;
                btnDel.DefaultCellStyle.BackColor = Color.LightPink;
                dgvEmpleados.Columns.Add(btnDel);
            }
        }

        // Limpia la búsqueda y recarga todos los empleados
        private void LimpiarBusqueda()
        {
            if (txtBuscarNombre != null)
            {
                txtBuscarNombre.Clear();
                txtBuscarNombre.Focus();
            }
            CargarListaEmpleados();
        }

        // Maneja el clic en el campo de búsqueda para mostrar sugerencias
        private void txtBuscarNombre_Click(object sender, EventArgs e)
        {
            if (!(sender is TextBox textBox)) return;

            try
            {
                // Crear menú contextual con sugerencias de búsqueda
                var menu = new ContextMenuStrip();

                // Opción para limpiar búsqueda
                menu.Items.Add(new ToolStripMenuItem("Limpiar búsqueda", null, (s2, e2) => {
                    textBox.Clear();
                    textBox.Focus();
                }));
                menu.Items.Add(new ToolStripSeparator());

                // Sugerencias por categoría
                menu.Items.Add(new ToolStripMenuItem("Buscar por nombre:", null) { Enabled = false, BackColor = Color.LightBlue });
                menu.Items.Add(new ToolStripMenuItem("   Juan, María, Pedro", null, (s2, e2) => {
                    textBox.Text = "Juan";
                    textBox.SelectAll();
                }));

                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(new ToolStripMenuItem("Buscar por departamento:", null) { Enabled = false, BackColor = Color.LightGreen });

                var departamentos = ObtenerDepartamentosDisponibles();
                foreach (var dept in departamentos.Take(3))
                {
                    var departamento = dept;
                    menu.Items.Add(new ToolStripMenuItem($"   {departamento}", null, (s2, e2) => {
                        textBox.Text = departamento;
                        textBox.SelectAll();
                    }));
                }

                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(new ToolStripMenuItem("Buscar por habilidad:", null) { Enabled = false, BackColor = Color.LightYellow });

                var habilidades = ObtenerHabilidadesDisponibles();
                foreach (var hab in habilidades.Take(3))
                {
                    var habilidad = hab;
                    menu.Items.Add(new ToolStripMenuItem($"   {habilidad}", null, (s2, e2) => {
                        textBox.Text = habilidad;
                        textBox.SelectAll();
                    }));
                }

                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(new ToolStripMenuItem("Otros ejemplos:", null) { Enabled = false, BackColor = Color.LightCoral });
                menu.Items.Add(new ToolStripMenuItem("   @gmail. com (email)", null, (s2, e2) => {
                    textBox.Text = "@gmail.com";
                    textBox.SelectAll();
                }));
                menu.Items.Add(new ToolStripMenuItem("   791 (teléfono)", null, (s2, e2) => {
                    textBox.Text = "791";
                    textBox.SelectAll();
                }));

                menu.Show(textBox, new Point(0, textBox.Height));
            }
            catch
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        }

        // Obtiene la lista de departamentos disponibles para sugerencias
        private string[] ObtenerDepartamentosDisponibles()
        {
            try
            {
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null) return new string[] { "General", "IT / Sistemas", "Recursos Humanos", "Ventas" };

                    string query = @"
                        SELECT DISTINCT ISNULL(D.Departamento, 'General') AS Departamento
                        FROM dbo.Usuarios U
                        LEFT JOIN dbo.DatosPersonales D ON U.IdUsuario = D.IdUsuario
                        WHERE U.Rol IN ('Empleado', 'Admin')
                        AND ISNULL(D.Departamento, 'General') <> ''
                        ORDER BY Departamento";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        return dt.Rows.Cast<DataRow>().Select(r => r["Departamento"].ToString()).ToArray();
                    }
                }
            }
            catch
            {
                return new string[] { "General", "IT / Sistemas", "Recursos Humanos", "Ventas" };
            }
        }

        // Obtiene la lista de habilidades disponibles para sugerencias
        private string[] ObtenerHabilidadesDisponibles()
        {
            try
            {
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null) return new string[] { "C#", "Java", "Python", "Excel", "SQL Server" };

                    string query = @"
                        SELECT DISTINCT H.Habilidad
                        FROM dbo.Habilidades H
                        INNER JOIN dbo.Usuarios U ON H.IdUsuario = U.IdUsuario
                        WHERE U.Rol IN ('Empleado', 'Admin')
                        AND H.Habilidad IS NOT NULL 
                        AND H.Habilidad <> ''
                        ORDER BY H.Habilidad";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        return dt.Rows.Cast<DataRow>().Select(r => r["Habilidad"].ToString()).ToArray();
                    }
                }
            }
            catch
            {
                return new string[] { "C#", "Java", "Python", "Excel", "SQL Server" };
            }
        }

        // Maneja los clics en las celdas del DataGridView (botones de acción)
        private void dgvEmpleados_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!dgvEmpleados.Columns.Contains("IdUsuario")) return;

            int idEmpleado;
            try
            {
                idEmpleado = Convert.ToInt32(dgvEmpleados.Rows[e.RowIndex].Cells["IdUsuario"].Value);
            }
            catch
            {
                MessageBox.Show("No se pudo obtener el Id del usuario seleccionado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string nombre = dgvEmpleados.Rows[e.RowIndex].Cells["Nombre"].Value?.ToString() ?? "(sin nombre)";
            string estadoActual = dgvEmpleados.Rows[e.RowIndex].Cells["Estado"].Value?.ToString() ?? "";

            // Cambiar estado del usuario (Activo/Inactivo)
            if (dgvEmpleados.Columns[e.ColumnIndex].Name == "Eliminar")
            {
                if (estadoActual.Equals("Inactivo", StringComparison.OrdinalIgnoreCase))
                {
                    if (MessageBox.Show($"El usuario '{nombre}' está inactivo.\n¿Deseas REACTIVARLO?", "Reactivar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        CambiarEstado(idEmpleado, "Activo");
                    }
                }
                else
                {
                    if (MessageBox.Show($"¿Dar de baja a '{nombre}'?", "Confirmar Baja", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        CambiarEstado(idEmpleado, "Inactivo");
                    }
                }
                return;
            }

            // Ver/Editar CV del empleado
            if (dgvEmpleados.Columns[e.ColumnIndex].Name == "VerCV")
            {
                try
                {
                    bool datosCompletos = CargarDatosEmpleadoCompleto(idEmpleado);

                    if (!datosCompletos)
                    {
                        MessageBox.Show("No se pudieron cargar los datos del usuario.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DatosCompartidos.IdUsuarioSeleccionado = idEmpleado;
                    DatosCompartidos.EsEdicionCompleta = true;

                    using (var verCVEmpleado = new VerCV())
                    {
                        verCVEmpleado.Text = $"CV de {nombre} - Modo Administrador";
                        verCVEmpleado.ShowDialog(this);
                    }

                    MessageBox.Show("CV cerrado correctamente.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al abrir CV: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    DatosCompartidos.Limpiar();
                    CargarListaEmpleados();
                }
            }
        }

        // Carga todos los datos del empleado seleccionado en DatosCompartidos
        private bool CargarDatosEmpleadoCompleto(int idEmpleado)
        {
            try
            {
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null) return false;

                    DatosCompartidos.Limpiar();

                    if (!CargarDatosPersonales(con, idEmpleado)) return false;

                    CargarFormacionAcademica(con, idEmpleado);
                    CargarExperienciaLaboral(con, idEmpleado);
                    CargarHabilidades(con, idEmpleado);
                    CargarReferencias(con, idEmpleado);

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos del usuario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Carga los datos personales del empleado
        private bool CargarDatosPersonales(SqlConnection con, int idEmpleado)
        {
            string query = @"
                SELECT 
                    D. Nombre, D.Telefono, D.Correo, D.Departamento, D. Objetivo, D.Foto,
                    U.Login
                FROM dbo.DatosPersonales D
                RIGHT JOIN dbo.Usuarios U ON D.IdUsuario = U. IdUsuario
                WHERE U.IdUsuario = @Id";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idEmpleado);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        DatosCompartidos.NombreCompleto = reader["Nombre"]?.ToString() ?? reader["Login"]?.ToString() ?? "";
                        DatosCompartidos.Telefono = reader["Telefono"]?.ToString() ?? "";
                        DatosCompartidos.Correo = reader["Correo"]?.ToString() ?? "";
                        DatosCompartidos.Departamento = reader["Departamento"]?.ToString() ?? "";
                        DatosCompartidos.ObjetivoProfesional = reader["Objetivo"]?.ToString() ?? "";

                        if (reader["Foto"] != DBNull.Value)
                        {
                            DatosCompartidos.Foto = (byte[])reader["Foto"];
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        // Carga la formación académica del empleado
        private void CargarFormacionAcademica(SqlConnection con, int idEmpleado)
        {
            string query = @"
                SELECT IdFormacion, Institucion, Titulo, Ubicacion, FechaInicio, FechaFin, TipoEstudio
                FROM FormacionAcademica 
                WHERE IdUsuario = @Id
                ORDER BY FechaInicio DESC";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idEmpleado);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DatosCompartidos.ListaEstudios.Add(new FormacionAcademica
                        {
                            IdFormacion = Convert.ToInt32(reader["IdFormacion"]),
                            Institucion = reader["Institucion"]?.ToString() ?? "",
                            Titulo = reader["Titulo"]?.ToString() ?? "",
                            Ubicacion = reader["Ubicacion"]?.ToString() ?? "",
                            FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                            FechaFin = Convert.ToDateTime(reader["FechaFin"]),
                            TipoEstudio = reader["TipoEstudio"]?.ToString() ?? ""
                        });
                    }
                }
            }
        }

        // Carga la experiencia laboral del empleado
        private void CargarExperienciaLaboral(SqlConnection con, int idEmpleado)
        {
            string query = @"
                SELECT IdExperiencia, Cargo, Empresa, FechaInicio, FechaFin
                FROM ExperienciaLaboral 
                WHERE IdUsuario = @Id
                ORDER BY FechaInicio DESC";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idEmpleado);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DatosCompartidos.ListaExperiencia.Add(new ExperienciaLaboral
                        {
                            IdExperiencia = Convert.ToInt32(reader["IdExperiencia"]),
                            Cargo = reader["Cargo"]?.ToString() ?? "",
                            Entidad = reader["Empresa"]?.ToString() ?? "",
                            FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                            FechaFin = Convert.ToDateTime(reader["FechaFin"])
                        });
                    }
                }
            }
        }

        // Carga las habilidades del empleado
        private void CargarHabilidades(SqlConnection con, int idEmpleado)
        {
            string query = @"
                SELECT IdHabilidad, Habilidad, Competencia, Dominio
                FROM Habilidades 
                WHERE IdUsuario = @Id
                ORDER BY Habilidad";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idEmpleado);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DatosCompartidos.ListaHabilidades.Add(new Habilidad
                        {
                            IdHabilidad = Convert.ToInt32(reader["IdHabilidad"]),
                            Nombre = reader["Habilidad"]?.ToString() ?? "",
                            Competencia = reader["Competencia"]?.ToString() ?? "",
                            Dominio = reader["Dominio"]?.ToString() ?? ""
                        });
                    }
                }
            }
        }

        // Carga las referencias del empleado
        private void CargarReferencias(SqlConnection con, int idEmpleado)
        {
            string query = @"
                SELECT IdReferencia, Nombre, Telefono, Tipo
                FROM Referencias 
                WHERE IdUsuario = @Id
                ORDER BY Tipo, Nombre";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idEmpleado);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DatosCompartidos.ListaReferencias.Add(new Referencia
                        {
                            IdReferencia = Convert.ToInt32(reader["IdReferencia"]),
                            Nombre = reader["Nombre"]?.ToString() ?? "",
                            Telefono = reader["Telefono"]?.ToString() ?? "",
                            Tipo = reader["Tipo"]?.ToString() ?? "Personal"
                        });
                    }
                }
            }
        }

        // Cambia el estado de un usuario (Activo/Inactivo)
        private void CambiarEstado(int id, string estado)
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                if (con == null)
                {
                    MessageBox.Show("No hay conexión a la base de datos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    string q = "UPDATE Usuarios SET Estado = @estado WHERE IdUsuario = @id";
                    using (SqlCommand cmd = new SqlCommand(q, con))
                    {
                        cmd.Parameters.AddWithValue("@estado", estado);
                        cmd.Parameters.AddWithValue("@id", id);
                        int filasAfectadas = cmd.ExecuteNonQuery();

                        if (filasAfectadas > 0)
                        {
                            MessageBox.Show($"Estado cambiado a: {estado}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("No se pudo cambiar el estado.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    CargarListaEmpleados();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cambiar estado: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Eventos de los botones del menú principal

        // Ejecuta la búsqueda manualmente
        private void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarListaEmpleados();
        }

        // Abre el formulario para crear un nuevo CV
        private void btnCrearCV_Click(object sender, EventArgs e)
        {
            DatosCompartidos.Limpiar();
            using (var frm = new Crear_CV(this))
            {
                frm.ShowDialog(this);
            }
            CargarListaEmpleados();
        }

        // Abre el formulario para ver el CV del usuario actual
        private void btnVerCV_Click(object sender, EventArgs e)
        {
            DatosCompartidos.IdUsuarioSeleccionado = 0;
            using (var verCV = new VerCV())
            {
                verCV.ShowDialog(this);
            }
            DatosCompartidos.Limpiar();
        }

        // Cierra la sesión y regresa al login
        private void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            DatosCompartidos.Limpiar();
            SesionActual.CerrarSesion();
            new Login().Show();
            this.Close();
        }

        // Botón de cerrar aplicación (sin funcionalidad)
        private void buttonClose_Click(object sender, EventArgs e)
        {
            // Funcionalidad para cerrar aplicación puede ir aquí
        }

        // Evento de carga del botón empleados (sin funcionalidad)
        private void btnEmpleados_Load(object sender, EventArgs e) { }

        // Regresa al formulario anterior
        private void btnAtras_Click(object sender, EventArgs e)
        {
            if (formAnterior != null)
            {
                formAnterior.Show();
                this.Close();
            }
            else
            {
                this.Close();
            }
        }

        // Método alternativo para cerrar sesión (método duplicado)
        private void btnCerrarSesion_Click_1(object sender, EventArgs e)
        {
            try
            {
                // Confirmar cierre de sesión
                DialogResult confirmacion = MessageBox.Show(
                    "¿Está seguro que desea cerrar la sesión?",
                    "Confirmar Cierre de Sesión",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmacion != DialogResult.Yes)
                    return;

                // Limpiar datos compartidos
                DatosCompartidos.Limpiar();

                // Cerrar sesión actual
                SesionActual.CerrarSesion();

                // Mostrar formulario de login
                Login loginForm = new Login();
                loginForm.Show();

                // Cerrar completamente el formulario actual
                this.Hide();
                this.Close();
                this.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cerrar sesión: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEquipo_Click(object sender, EventArgs e)
        {
            Equipo equipo = new Equipo();
            equipo.Show();
            this.Hide();

        }
    }
}