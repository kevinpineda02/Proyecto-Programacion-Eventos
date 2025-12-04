using GestorRRHH.Base_de_datos;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace GestorRRHH.Agregar_CV
{
    // Formulario encargado de visualizar la información completa del Currículum Vitae.
    // Permite ver, editar y eliminar registros (estudios, experiencia, habilidades, etc.)
    // y exportar el documento final a PDF.
    public partial class VerCV : Form
    {
        // Variable que almacena el ID del usuario del cual se está viendo el CV.
        private int usuarioIdObjetivo;

        public VerCV()
        {
            InitializeComponent();
        }

        private void VerCV_Load(object sender, EventArgs e)
        {
            // Evento de carga por defecto (se utiliza VerCV_Load_1 más abajo).
        }

        // Método principal para recuperar los datos de la base de datos y llenar las tablas visuales.
        private void CargarTodasLasTablas()
        {
            try
            {
                // Establece la conexión con la base de datos.
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null)
                    {
                        MessageBox.Show("No se pudo conectar a la base de datos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Se cargan los datos personales (Nombre, teléfono, objetivo, etc.).
                    LlenarGrid(dgvDatosPersonales, "SELECT IdUsuario, Nombre, Telefono, Correo, Departamento, Objetivo FROM DatosPersonales WHERE IdUsuario = @Id", con);

                    // Se carga la lista de formación académica.
                    LlenarGrid(dgvEstudios, "SELECT * FROM FormacionAcademica WHERE IdUsuario = @Id", con);

                    // Se carga el historial de experiencia laboral.
                    LlenarGrid(dgvExperiencia, "SELECT * FROM ExperienciaLaboral WHERE IdUsuario = @Id", con);

                    // Se cargan las habilidades registradas.
                    LlenarGrid(dgvHabilidades, "SELECT * FROM Habilidades WHERE IdUsuario = @Id", con);

                    // Se cargan las referencias personales o laborales.
                    LlenarGrid(dgvReferencias, "SELECT * FROM Referencias WHERE IdUsuario = @Id", con);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las tablas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método genérico para configurar y llenar cualquier DataGridView con una consulta SQL.
        private void LlenarGrid(DataGridView grid, string query, SqlConnection con)
        {
            try
            {
                if (grid == null) return;
                grid.Columns.Clear();

                // Ejecución de la consulta y llenado del DataTable.
                using (SqlDataAdapter da = new SqlDataAdapter(query, con))
                {
                    da.SelectCommand.Parameters.AddWithValue("@Id", usuarioIdObjetivo);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    grid.DataSource = dt;
                }

                // Configuración del estilo visual de la tabla.
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                grid.RowHeadersVisible = false;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.AllowUserToAddRows = false;
                grid.ReadOnly = true;
                grid.BackgroundColor = Color.White;
                grid.BorderStyle = BorderStyle.None;

                // Se ocultan las columnas de ID (claves primarias) y se formatean las fechas.
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (col.Name.Contains("Id") || col.Name.Contains("ID"))
                        col.Visible = false;

                    if (col.Name.Contains("Fecha") || col.Name.Contains("Date"))
                        col.DefaultCellStyle.Format = "dd/MM/yyyy";
                }

                // Si el usuario tiene permisos, se agregan los botones de acción (Editar/Eliminar).
                if (TienePermisoEditar())
                {
                    // Se usa texto "EDITAR" en lugar de emojis.
                    AgregarBotonGrid(grid, "Editar", "EDITAR", Color.Blue);

                    // En datos personales no se permite eliminar la fila completa, solo editarla.
                    if (grid.Name != "dgvDatosPersonales")
                    {
                        // Se usa texto "ELIMINAR" en lugar de emojis.
                        AgregarBotonGrid(grid, "Eliminar", "ELIMINAR", Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando tabla: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método auxiliar para agregar una columna de botones a la tabla.
        private void AgregarBotonGrid(DataGridView grid, string nombre, string texto, Color color)
        {
            if (!grid.Columns.Contains(nombre))
            {
                DataGridViewButtonColumn btn = new DataGridViewButtonColumn
                {
                    Name = nombre,
                    HeaderText = "",
                    Text = texto,
                    UseColumnTextForButtonValue = true,
                    Width = 70, // Se aumenta un poco el ancho para que quepa el texto
                    FlatStyle = FlatStyle.Flat
                };
                btn.DefaultCellStyle.ForeColor = color;
                grid.Columns.Add(btn);
            }
        }

        // Verifica si el usuario actual tiene permisos para modificar la información mostrada.
        private bool TienePermisoEditar()
        {
            // El Administrador puede editar cualquier perfil.
            // El Empleado solo puede editar su propio perfil.
            if (SesionActual.Rol == "Admin") return true;
            return SesionActual.Rol == "Empleado" && SesionActual.IdUsuario == usuarioIdObjetivo;
        }

        // ====================================================================
        //  LOGICA DE CLICS EN CADA TABLA (EDITAR / ELIMINAR)
        // ====================================================================

        // Método centralizado para manejar los clics en los botones de las tablas.
        // Identifica si se quiere Editar o Eliminar y ejecuta la acción correspondiente.
        private void ProcesarAccion(DataGridView grid, DataGridViewCellEventArgs e, string tabla, string idColumna, Action<int> accionEditar)
        {
            try
            {
                if (e.RowIndex < 0) return;

                string accion = grid.Columns[e.ColumnIndex].Name;

                // Se intenta obtener el ID del registro seleccionado.
                int idRegistro = 0;
                if (grid.Columns.Contains(idColumna))
                {
                    if (grid.Rows[e.RowIndex].Cells[idColumna].Value != null)
                        idRegistro = Convert.ToInt32(grid.Rows[e.RowIndex].Cells[idColumna].Value);
                }
                else
                {
                    // Si no se encuentra la columna por nombre, se intenta con la primera columna (índice 0).
                    if (grid.Rows[e.RowIndex].Cells[0].Value != null)
                        idRegistro = Convert.ToInt32(grid.Rows[e.RowIndex].Cells[0].Value);
                }

                if (idRegistro == 0)
                {
                    MessageBox.Show("No se pudo identificar el registro.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Lógica para Eliminar registro.
                if (accion == "Eliminar")
                {
                    if (!TienePermisoEditar()) return;

                    if (MessageBox.Show("¿Eliminar este registro permanentemente?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        using (SqlConnection con = ConexionBD.ObtenerConexion())
                        {
                            if (con == null) return;

                            string query = $"DELETE FROM {tabla} WHERE {idColumna} = @IdReg";
                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@IdReg", idRegistro);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        MessageBox.Show("Registro eliminado correctamente.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        CargarTodasLasTablas(); // Se refrescan las tablas para mostrar los cambios.
                    }
                }
                // Lógica para Editar registro.
                else if (accion == "Editar")
                {
                    if (!TienePermisoEditar()) return;

                    // 1. Limpiamos la memoria compartida para evitar datos residuales.
                    DatosCompartidos.Limpiar();
                    DatosCompartidos.IdUsuarioSeleccionado = usuarioIdObjetivo;
                    DatosCompartidos.EsEdicionCompleta = false; // Se marca como edición parcial (quirúrgica).

                    // 2. Es CRÍTICO cargar los datos personales básicos en memoria para que no falle la validación del formulario de edición.
                    CargarDatosPersonalesEnMemoria();

                    // 3. Cargar las listas completas en memoria.
                    CargarListasEnMemoria();

                    // 4. Se ejecuta la acción específica pasada por parámetro (preparar el objeto a editar).
                    accionEditar(idRegistro);

                    // 5. Se abre el formulario de creación en modo edición.
                    GestorRRHH.Crear_CV editor = new GestorRRHH.Crear_CV(this);
                    this.Hide();
                    editor.ShowDialog(); // Se espera a que cierre el editor.
                    this.Show();
                    CargarTodasLasTablas(); // Se refrescan los datos al volver.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar acción: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- EVENTOS ESPECÍFICOS POR TABLA ---

        // Evento para editar Datos Personales.
        private void dgvDatosPersonales_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // Datos personales es un caso especial: Carga todo y activa el modo "Edición Completa".
                if (e.RowIndex >= 0 && dgvDatosPersonales.Columns[e.ColumnIndex].Name == "Editar")
                {
                    DatosCompartidos.Limpiar();
                    DatosCompartidos.IdUsuarioSeleccionado = usuarioIdObjetivo;

                    // Se cargan todos los datos actuales a la memoria estática.
                    CargarDatosPersonalesEnMemoria();
                    CargarListasEnMemoria();

                    DatosCompartidos.EsEdicionCompleta = true; // Habilita el asistente completo paso a paso.

                    GestorRRHH.Crear_CV editor = new GestorRRHH.Crear_CV(this);
                    this.Hide();
                    editor.ShowDialog();
                    this.Show();
                    CargarTodasLasTablas();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar datos personales: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Evento para editar/eliminar Estudios.
        private void dgvEstudios_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            ProcesarAccion(dgvEstudios, e, "FormacionAcademica", "IdFormacion", (id) =>
            {
                var row = dgvEstudios.Rows[e.RowIndex];
                DatosCompartidos.EstudioAEditar = new FormacionAcademica
                {
                    IdFormacion = id,
                    Institucion = row.Cells["Institucion"].Value?.ToString() ?? "",
                    Titulo = row.Cells["Titulo"].Value?.ToString() ?? "",
                    Ubicacion = row.Cells["Ubicacion"].Value?.ToString() ?? "",
                    TipoEstudio = row.Cells["TipoEstudio"].Value?.ToString() ?? "",
                    FechaInicio = Convert.ToDateTime(row.Cells["FechaInicio"].Value),
                    FechaFin = Convert.ToDateTime(row.Cells["FechaFin"].Value)
                };
            });
        }

        // Evento para editar/eliminar Experiencia Laboral.
        private void dgvExperiencia_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            ProcesarAccion(dgvExperiencia, e, "ExperienciaLaboral", "IdExperiencia", (id) =>
            {
                var row = dgvExperiencia.Rows[e.RowIndex];
                DatosCompartidos.ExperienciaAEditar = new ExperienciaLaboral
                {
                    IdExperiencia = id,
                    Cargo = row.Cells["Cargo"].Value?.ToString() ?? "",
                    Entidad = row.Cells["Empresa"].Value?.ToString() ?? "",
                    FechaInicio = Convert.ToDateTime(row.Cells["FechaInicio"].Value),
                    FechaFin = Convert.ToDateTime(row.Cells["FechaFin"].Value)
                };
            });
        }

        // Evento para editar/eliminar Habilidades.
        private void dgvHabilidades_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            ProcesarAccion(dgvHabilidades, e, "Habilidades", "IdHabilidad", (id) =>
            {
                var row = dgvHabilidades.Rows[e.RowIndex];
                DatosCompartidos.HabilidadAEditar = new Habilidad
                {
                    IdHabilidad = id,
                    Nombre = row.Cells["Habilidad"].Value?.ToString() ?? "",
                    Competencia = row.Cells["Competencia"].Value?.ToString() ?? "",
                    Dominio = row.Cells["Dominio"].Value?.ToString() ?? ""
                };
            });
        }

        // Evento para editar/eliminar Referencias.
        private void dgvReferencias_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            ProcesarAccion(dgvReferencias, e, "Referencias", "IdReferencia", (id) =>
            {
                var row = dgvReferencias.Rows[e.RowIndex];
                DatosCompartidos.ReferenciaAEditar = new Referencia
                {
                    IdReferencia = id,
                    Nombre = row.Cells["Nombre"].Value?.ToString() ?? "",
                    Telefono = row.Cells["Telefono"].Value?.ToString() ?? "",
                    Tipo = row.Cells["Tipo"].Value?.ToString() ?? ""
                };
            });
        }

        // Método auxiliar que lee los datos personales de la BD y los guarda en la clase estática DatosCompartidos.
        private void CargarDatosPersonalesEnMemoria()
        {
            try
            {
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con != null)
                    {
                        string query = "SELECT Nombre, Telefono, Correo, Departamento, Objetivo, Foto FROM DatosPersonales WHERE IdUsuario = @Id";
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@Id", usuarioIdObjetivo);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    DatosCompartidos.NombreCompleto = reader["Nombre"]?.ToString() ?? "";
                                    DatosCompartidos.Telefono = reader["Telefono"]?.ToString() ?? "";
                                    DatosCompartidos.Correo = reader["Correo"]?.ToString() ?? "";
                                    DatosCompartidos.Departamento = reader["Departamento"]?.ToString() ?? "";
                                    DatosCompartidos.ObjetivoProfesional = reader["Objetivo"]?.ToString() ?? "";

                                    // Lectura de la foto (campo binario).
                                    if (reader["Foto"] != DBNull.Value)
                                    {
                                        DatosCompartidos.Foto = (byte[])reader["Foto"];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos personales: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método para cargar todas las listas (estudios, experiencia, etc.) en memoria para su edición.
        private void CargarListasEnMemoria()
        {
            try
            {
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null) return;

                    // Carga de estudios.
                    DatosCompartidos.ListaEstudios = new List<FormacionAcademica>();
                    string queryEstudios = "SELECT * FROM FormacionAcademica WHERE IdUsuario = @Id";
                    using (SqlCommand cmd = new SqlCommand(queryEstudios, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", usuarioIdObjetivo);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DatosCompartidos.ListaEstudios.Add(new FormacionAcademica
                                {
                                    IdFormacion = reader.GetInt32(reader.GetOrdinal("IdFormacion")),
                                    Institucion = reader["Institucion"]?.ToString() ?? "",
                                    Titulo = reader["Titulo"]?.ToString() ?? "",
                                    Ubicacion = reader["Ubicacion"]?.ToString() ?? "",
                                    TipoEstudio = reader["TipoEstudio"]?.ToString() ?? "",
                                    FechaInicio = reader.GetDateTime(reader.GetOrdinal("FechaInicio")),
                                    FechaFin = reader.GetDateTime(reader.GetOrdinal("FechaFin"))
                                });
                            }
                        }
                    }

                    // Carga de experiencia laboral.
                    DatosCompartidos.ListaExperiencia = new List<ExperienciaLaboral>();
                    string queryExp = "SELECT * FROM ExperienciaLaboral WHERE IdUsuario = @Id";
                    using (SqlCommand cmd = new SqlCommand(queryExp, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", usuarioIdObjetivo);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DatosCompartidos.ListaExperiencia.Add(new ExperienciaLaboral
                                {
                                    IdExperiencia = reader.GetInt32(reader.GetOrdinal("IdExperiencia")),
                                    Cargo = reader["Cargo"]?.ToString() ?? "",
                                    Entidad = reader["Empresa"]?.ToString() ?? "",
                                    FechaInicio = reader.GetDateTime(reader.GetOrdinal("FechaInicio")),
                                    FechaFin = reader.GetDateTime(reader.GetOrdinal("FechaFin"))
                                });
                            }
                        }
                    }

                    // Carga de habilidades.
                    DatosCompartidos.ListaHabilidades = new List<Habilidad>();
                    string queryHab = "SELECT * FROM Habilidades WHERE IdUsuario = @Id";
                    using (SqlCommand cmd = new SqlCommand(queryHab, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", usuarioIdObjetivo);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DatosCompartidos.ListaHabilidades.Add(new Habilidad
                                {
                                    IdHabilidad = reader.GetInt32(reader.GetOrdinal("IdHabilidad")),
                                    Nombre = reader["Habilidad"]?.ToString() ?? "",
                                    Competencia = reader["Competencia"]?.ToString() ?? "",
                                    Dominio = reader["Dominio"]?.ToString() ?? ""
                                });
                            }
                        }
                    }

                    // Carga de referencias.
                    DatosCompartidos.ListaReferencias = new List<Referencia>();
                    string queryRef = "SELECT * FROM Referencias WHERE IdUsuario = @Id";
                    using (SqlCommand cmd = new SqlCommand(queryRef, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", usuarioIdObjetivo);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DatosCompartidos.ListaReferencias.Add(new Referencia
                                {
                                    IdReferencia = reader.GetInt32(reader.GetOrdinal("IdReferencia")),
                                    Nombre = reader["Nombre"]?.ToString() ?? "",
                                    Telefono = reader["Telefono"]?.ToString() ?? "",
                                    Tipo = reader["Tipo"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar listas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Botón para volver atrás. Limpia la selección y regresa al formulario anterior.
        private void btnAtras_Click(object sender, EventArgs e)
        {
            Crear_CV crear_CV = new Crear_CV();
            crear_CV.Show();
            DatosCompartidos.Limpiar();
            this.Close();
            this.Hide();
        }

        // Evento de carga configurado para inicializar el scroll y determinar qué usuario mostrar.
        private void VerCV_Load_1(object sender, EventArgs e)
        {
            // Configuración de tamaño y barras de desplazamiento.
            this.Size = new Size(1352, 945);
            this.AutoScroll = true;
            this.HorizontalScroll.Maximum = 0;
            this.HorizontalScroll.Visible = true;
            this.VerticalScroll.Visible = true;
            this.AutoScroll = true;

            try
            {
                // 1. Lógica para determinar qué ID cargar.
                // Prioridad: Selección del Admin > Usuario actualmente logueado.
                if (DatosCompartidos.IdUsuarioSeleccionado != 0)
                {
                    usuarioIdObjetivo = DatosCompartidos.IdUsuarioSeleccionado;
                }
                else
                {
                    usuarioIdObjetivo = SesionActual.IdUsuario;
                }

                // Validación para asegurar que hay un usuario válido.
                if (usuarioIdObjetivo == 0)
                {
                    MessageBox.Show("No se pudo identificar el usuario. Por favor, inicie sesión.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                this.Text = $"Visualizando CV - Usuario ID: {usuarioIdObjetivo}";

                // 2. Se cargan los datos en la interfaz.
                CargarTodasLasTablas();

                // 3. Configuración inicial del botón de exportar.
                ConfigurarBotonExportar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el formulario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Configuración visual del botón Exportar (Tooltips).
        private void ConfigurarBotonExportar()
        {
            try
            {
                if (bttnExportar != null)
                {
                    ToolTip tooltip = new ToolTip();
                    tooltip.SetToolTip(bttnExportar, "Exportar CV completo a archivo PDF");
                }
            }
            catch
            {
                // Si hay error con la configuración visual, se ignora para no detener el flujo.
            }
        }

        // Evento principal para exportar el CV a formato PDF.
        private void bttnExportar_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Validaciones iniciales de integridad de datos.
                if (usuarioIdObjetivo == 0)
                {
                    MessageBox.Show("No hay datos de usuario para exportar.", "Error",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 2. Verificación de permisos de acceso.
                if (!TienePermisoVerCV())
                {
                    MessageBox.Show("No tiene permisos para exportar este CV.", "Acceso Denegado",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 3. Obtención de información básica para el mensaje de confirmación.
                string nombreUsuario = ObtenerNombreUsuario(usuarioIdObjetivo);
                string departamento = ObtenerDepartamentoUsuario(usuarioIdObjetivo);

                // 4. Diálogo de confirmación para el usuario.
                DialogResult confirmacion = MessageBox.Show(
                    $"CV a exportar:\n\n" +
                    $"- Usuario: {nombreUsuario}\n" +
                    $"- Departamento: {departamento}\n" +
                    $"- ID: {usuarioIdObjetivo}\n\n" +
                    $"¿Desea continuar con la exportación?",
                    "Exportar CV a PDF",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (confirmacion != DialogResult.Yes)
                    return;

                // 5. Verificación de que existen datos reales para exportar (evitar PDFs vacíos).
                if (!VerificarDatosParaExportar(usuarioIdObjetivo))
                {
                    DialogResult continuar = MessageBox.Show(
                        "El usuario no tiene datos completos en su CV.\n\n" +
                        "¿Desea exportar de todos modos?",
                        "Datos Incompletos",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (continuar != DialogResult.Yes)
                        return;
                }

                // 6. Indicador visual de "Cargando..."
                string textoOriginal = bttnExportar.Text;
                this.Cursor = Cursors.WaitCursor;
                bttnExportar.Text = "Generando PDF...";
                bttnExportar.Enabled = false;

                // Refresco de la interfaz de usuario.
                this.Refresh();
                Application.DoEvents();

                try
                {
                    // 7. Llamada a la clase Generadora de PDF.
                    bool exito = GestorRRHH.PDF.GeneradorCVPDF.GenerarCVPDF(usuarioIdObjetivo);

                    if (!exito)
                    {
                        MessageBox.Show(
                            "No se pudo generar el PDF.\n\n" +
                            "Posibles causas:\n" +
                            "- Datos incompletos\n" +
                            "- Error en la ruta de destino\n" +
                            "- Problemas de permisos",
                            "Error en Exportación",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }
                    // Si exito es true, la clase GeneradorCVPDF se encarga de mostrar el mensaje de éxito.
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error durante la generación del PDF:\n\n{ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                finally
                {
                    // 8. Restauración del estado del botón y cursor (Bloque Finally para asegurar ejecución).
                    this.Cursor = Cursors.Default;
                    bttnExportar.Text = textoOriginal;
                    bttnExportar.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al iniciar la exportación:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // Restauración de emergencia del botón.
                this.Cursor = Cursors.Default;
                bttnExportar.Text = "Exportar PDF";
                bttnExportar.Enabled = true;
            }
        }

        // --- MÉTODOS AUXILIARES PARA EXPORTACIÓN ---

        // Verifica si el usuario actual tiene rol o permisos para exportar este documento.
        private bool TienePermisoVerCV()
        {
            // El administrador tiene acceso total.
            if (SesionActual.Rol == "Admin") return true;

            // El empleado solo tiene acceso a su propio documento.
            return SesionActual.Rol == "Empleado" && SesionActual.IdUsuario == usuarioIdObjetivo;
        }

        // Obtiene el nombre del usuario desde la base de datos para mostrarlo en los mensajes.
        private string ObtenerNombreUsuario(int idUsuario)
        {
            try
            {
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null) return "Usuario";

                    string query = @"
                        SELECT ISNULL(D.Nombre, U.Login) AS Nombre 
                        FROM Usuarios U 
                        LEFT JOIN DatosPersonales D ON U.IdUsuario = D.IdUsuario 
                        WHERE U.IdUsuario = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", idUsuario);
                        object resultado = cmd.ExecuteScalar();
                        return resultado?.ToString() ?? "Usuario";
                    }
                }
            }
            catch
            {
                return "Usuario";
            }
        }

        // Obtiene el nombre del departamento del usuario.
        private string ObtenerDepartamentoUsuario(int idUsuario)
        {
            try
            {
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null) return "No especificado";

                    string query = "SELECT ISNULL(Departamento, 'No especificado') FROM DatosPersonales WHERE IdUsuario = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", idUsuario);
                        object resultado = cmd.ExecuteScalar();
                        return resultado?.ToString() ?? "No especificado";
                    }
                }
            }
            catch
            {
                return "No especificado";
            }
        }

        // Realiza una consulta rápida para verificar si el usuario tiene al menos su nombre registrado.
        private bool VerificarDatosParaExportar(int idUsuario)
        {
            try
            {
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null) return false;

                    // Se comprueba si existe un registro en DatosPersonales con nombre no nulo.
                    string query = @"
                        SELECT COUNT(*) 
                        FROM DatosPersonales 
                        WHERE IdUsuario = @Id 
                        AND Nombre IS NOT NULL 
                        AND Nombre <> ''";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", idUsuario);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                // En caso de fallo de conexión, se asume que no hay datos disponibles.
                return false;
            }
        }
    }
}