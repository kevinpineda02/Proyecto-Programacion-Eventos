using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GestorRRHH.Agregar_CV;
using GestorRRHH.Base_de_datos;

namespace GestorRRHH
{
    // Formulario encargado de la creacion y edicion de Curriculums.
    // Maneja tanto la creacion de un CV nuevo como la edicion completa o parcial (un solo registro).
    public partial class Crear_CV : Form
    {
        // Listas locales que actuan como buffer temporal para acumular datos 
        // (Estudios, Experiencia, etc.) antes de confirmar el guardado en la base de datos.
        private List<FormacionAcademica> listaEstudiosLocal = new List<FormacionAcademica>();
        private List<ExperienciaLaboral> listaExperienciaLocal = new List<ExperienciaLaboral>();
        private List<Habilidad> listaHabilidadesLocal = new List<Habilidad>();
        private List<Referencia> listaReferenciasLocal = new List<Referencia>();

        // Almacena la fotografia en formato de arreglo de bytes para su envio a la BD.
        private byte[] fotoBytes = null;

        // Propiedad para controlar el nivel de restricciones en la edicion.
        public bool ModoEdicionEstricta { get; set; } = false;

        // Variable que almacena la referencia al formulario anterior para permitir la navegacion de retorno.
        private Form formularioAnterior;

        // Constructor que recibe el formulario padre/anterior.
        public Crear_CV(Form anterior)
        {
            InitializeComponent();
            ConfigurarScroll();
            formularioAnterior = anterior;
        }

        // Constructor por defecto sin referencia al formulario anterior.
        public Crear_CV()
        {
            InitializeComponent();
            ConfigurarScroll();
            formularioAnterior = null;
        }

        // Configuracion de las propiedades de desplazamiento (scroll) y tamaño de la ventana.
        private void ConfigurarScroll()
        {
            this.Size = new Size(1352, 945);
            this.AutoScroll = true;
            this.HorizontalScroll.Visible = true;
            this.VerticalScroll.Visible = true;
            this.AutoScroll = true;
        }

        // Evento de carga del formulario. Inicializa los controles y carga datos si existen en memoria compartida.
        private void Agregar_CV_Load(object sender, EventArgs e)
        {
            // La opcion de ver empleados solo esta visible para el rol de Administrador.
            btnEmpleado.Visible = (SesionActual.Rol == "Admin");

            try
            {
                // Verificacion de existencia de datos compartidos para determinar si es una edicion o una creacion.
                // Se comprueba si hay datos personales, listas o elementos especificos seleccionados para editar.
                bool hayDatosCompartidos = !string.IsNullOrEmpty(DatosCompartidos.NombreCompleto) ||
                                            DatosCompartidos.ListaEstudios?.Count > 0 ||
                                            DatosCompartidos.ListaExperiencia?.Count > 0 ||
                                            DatosCompartidos.EstudioAEditar != null ||
                                            DatosCompartidos.ExperienciaAEditar != null ||
                                            DatosCompartidos.HabilidadAEditar != null ||
                                            DatosCompartidos.ReferenciaAEditar != null;

                // 1. Cargar Datos Personales si existen en la memoria compartida.
                if (hayDatosCompartidos)
                {
                    txtNombreCompleto.Text = DatosCompartidos.NombreCompleto ?? "";
                    txtTelefono.Text = DatosCompartidos.Telefono ?? "";
                    txtCorreo.Text = DatosCompartidos.Correo ?? "";
                    boxDepartamento.Text = DatosCompartidos.Departamento ?? "";
                    TxtObjetivo.Text = DatosCompartidos.ObjetivoProfesional ?? "";

                    if (DatosCompartidos.Foto != null)
                        fotoBytes = DatosCompartidos.Foto;

                    // Recuperar listas existentes SOLO si NO estamos editando un item especifico (Modo Edicion Completa).
                    if (EsModoEdicionCompleta())
                    {
                        if (DatosCompartidos.ListaEstudios != null)
                            listaEstudiosLocal = new List<FormacionAcademica>(DatosCompartidos.ListaEstudios);
                        if (DatosCompartidos.ListaExperiencia != null)
                            listaExperienciaLocal = new List<ExperienciaLaboral>(DatosCompartidos.ListaExperiencia);
                        if (DatosCompartidos.ListaHabilidades != null)
                            listaHabilidadesLocal = new List<Habilidad>(DatosCompartidos.ListaHabilidades);
                        if (DatosCompartidos.ListaReferencias != null)
                            listaReferenciasLocal = new List<Referencia>(DatosCompartidos.ListaReferencias);
                    }
                    else
                    {
                        // En modo edicion especifica, se cargan las listas excluyendo el elemento que se esta modificando actualmente.
                        CargarListasSinItemEditando();
                    }

                    // 2. Logica de Bloqueo para edicion puntual (Modo Quirurgico).
                    // Se ejecuta solo si hay datos compartidos y NO es una edicion completa.
                    if (!DatosCompartidos.EsEdicionCompleta)
                    {
                        // Primero se bloquean todos los controles de la interfaz.
                        BloquearTodo();

                        // Luego se desbloquea y rellenan los datos unicamente de la seccion especifica a editar.
                        if (DatosCompartidos.EstudioAEditar != null)
                        {
                            DesbloquearSoloEstudios();
                            var est = DatosCompartidos.EstudioAEditar;
                            txtInstitucion.Text = est.Institucion ?? "";
                            txtTitulo.Text = est.Titulo ?? "";
                            txtUbicacion.Text = est.Ubicacion ?? "";
                            dateFechaInicio.Value = est.FechaInicio;
                            dateFechaFinalizacion.Value = est.FechaFin;
                            txtDiplomado.Text = est.TipoEstudio ?? "";
                        }
                        else if (DatosCompartidos.ExperienciaAEditar != null)
                        {
                            DesbloquearSoloExperiencia();
                            var exp = DatosCompartidos.ExperienciaAEditar;
                            txtCargo.Text = exp.Cargo ?? "";
                            txtEntidad.Text = exp.Entidad ?? "";
                            dateEntrada.Value = exp.FechaInicio;
                            dateSalida.Value = exp.FechaFin;
                        }
                        else if (DatosCompartidos.HabilidadAEditar != null)
                        {
                            DesbloquearSoloHabilidades();
                            var hab = DatosCompartidos.HabilidadAEditar;
                            txtHabilidad.Text = hab.Nombre ?? "";
                            txtCompetencia.Text = hab.Competencia ?? "";
                            txtDominio.Text = hab.Dominio ?? "";
                        }
                        else if (DatosCompartidos.ReferenciaAEditar != null)
                        {
                            DesbloquearSoloReferencias();
                            var refer = DatosCompartidos.ReferenciaAEditar;

                            // Se determina si es referencia personal o laboral para llenar los campos correctos.
                            if (refer.Tipo == "Personal")
                            {
                                txtRefPersonalNombre.Text = refer.Nombre ?? "";
                                txtRefPersonalTelefono.Text = refer.Telefono ?? "";
                            }
                            else if (refer.Tipo == "Laboral")
                            {
                                txtRefLaboralNombre.Text = refer.Nombre ?? "";
                                txtRefLaboralTelefono.Text = refer.Telefono ?? "";
                            }
                            else
                            {
                                // Valor por defecto si no se especifica tipo.
                                txtRefPersonalNombre.Text = refer.Nombre ?? "";
                                txtRefPersonalTelefono.Text = refer.Telefono ?? "";
                            }
                        }
                    }
                }
                else
                {
                    // Si NO hay datos compartidos, se asume que es un CV completamente nuevo.
                    // Se habilitan todos los controles y se limpian los campos.
                    HabilitarTodoParaCreacion();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el formulario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- METODOS AUXILIARES PARA DETERMINAR MODO DE EDICION ---

        // Determina si el formulario esta en modo de "Edicion Completa" (CV entero) o "Edicion Especifica".
        // Devuelve verdadero si se esta creando un CV nuevo o si se selecciono "Editar Todo".
        private bool EsModoEdicionCompleta()
        {
            // Verificacion rapida: Si no hay datos compartidos, es una creacion nueva.
            bool hayDatosCompartidos = !string.IsNullOrEmpty(DatosCompartidos.NombreCompleto) ||
                                      DatosCompartidos.ListaEstudios?.Count > 0 ||
                                      DatosCompartidos.ListaExperiencia?.Count > 0 ||
                                      DatosCompartidos.EstudioAEditar != null ||
                                      DatosCompartidos.ExperienciaAEditar != null ||
                                      DatosCompartidos.HabilidadAEditar != null ||
                                      DatosCompartidos.ReferenciaAEditar != null;

            if (!hayDatosCompartidos)
                return true;

            // Es edicion completa si la bandera estatica lo indica O si no hay ningun objeto especifico seleccionado para editar.
            return DatosCompartidos.EsEdicionCompleta ||
                   (DatosCompartidos.EstudioAEditar == null &&
                    DatosCompartidos.ExperienciaAEditar == null &&
                    DatosCompartidos.HabilidadAEditar == null &&
                    DatosCompartidos.ReferenciaAEditar == null);
        }

        // Habilita todos los controles de la interfaz para permitir la creacion de un CV desde cero.
        private void HabilitarTodoParaCreacion()
        {
            // Habilitacion de Datos Personales
            txtNombreCompleto.Enabled = true;
            txtTelefono.Enabled = true;
            txtCorreo.Enabled = true;
            boxDepartamento.Enabled = true;
            TxtObjetivo.Enabled = true;
            bttnImagen.Enabled = true;

            // Habilitacion de Formacion Academica
            txtInstitucion.Enabled = true;
            txtTitulo.Enabled = true;
            txtUbicacion.Enabled = true;
            txtDiplomado.Enabled = true;
            dateFechaInicio.Enabled = true;
            dateFechaFinalizacion.Enabled = true;
            btnMas.Enabled = true;

            // Habilitacion de Experiencia Profesional
            txtCargo.Enabled = true;
            txtEntidad.Enabled = true;
            dateEntrada.Enabled = true;
            dateSalida.Enabled = true;

            // Habilitacion de Habilidades
            txtHabilidad.Enabled = true;
            txtCompetencia.Enabled = true;
            txtDominio.Enabled = true;

            // Habilitacion de Referencias
            txtRefPersonalNombre.Enabled = true;
            txtRefPersonalTelefono.Enabled = true;
            txtRefLaboralNombre.Enabled = true;
            txtRefLaboralTelefono.Enabled = true;

            // Se limpian todos los campos para asegurar un estado inicial limpio.
            LimpiarTodosLosCampos();
        }

        // Limpia el contenido de todos los cuadros de texto y reinicia las listas locales.
        private void LimpiarTodosLosCampos()
        {
            // Limpieza de Datos Personales
            txtNombreCompleto.Clear();
            txtTelefono.Clear();
            txtCorreo.Clear();
            boxDepartamento.ResetText();
            TxtObjetivo.Clear();

            // Limpieza de Formacion Academica
            txtInstitucion.Clear();
            txtTitulo.Clear();
            txtUbicacion.Clear();
            txtDiplomado.Clear();
            dateFechaInicio.Value = DateTime.Now;
            dateFechaFinalizacion.Value = DateTime.Now;

            // Limpieza de Experiencia Profesional
            txtCargo.Clear();
            txtEntidad.Clear();
            dateEntrada.Value = DateTime.Now;
            dateSalida.Value = DateTime.Now;

            // Limpieza de Habilidades
            txtHabilidad.Clear();
            txtCompetencia.Clear();
            txtDominio.Clear();

            // Limpieza de Referencias
            txtRefPersonalNombre.Clear();
            txtRefPersonalTelefono.Clear();
            txtRefLaboralNombre.Clear();
            txtRefLaboralTelefono.Clear();

            // Limpieza de variable de foto
            fotoBytes = null;

            // Reinicio de listas locales
            listaEstudiosLocal.Clear();
            listaExperienciaLocal.Clear();
            listaHabilidadesLocal.Clear();
            listaReferenciasLocal.Clear();
        }

        // Carga las listas locales con los datos compartidos, pero excluye el elemento que se esta editando actualmente.
        // Esto evita duplicados al guardar.
        private void CargarListasSinItemEditando()
        {
            // Filtrado de Estudios
            if (DatosCompartidos.ListaEstudios != null)
            {
                listaEstudiosLocal = new List<FormacionAcademica>();
                foreach (var item in DatosCompartidos.ListaEstudios)
                {
                    if (DatosCompartidos.EstudioAEditar == null || item.IdFormacion != DatosCompartidos.EstudioAEditar.IdFormacion)
                    {
                        listaEstudiosLocal.Add(item);
                    }
                }
            }

            // Filtrado de Experiencia
            if (DatosCompartidos.ListaExperiencia != null)
            {
                listaExperienciaLocal = new List<ExperienciaLaboral>();
                foreach (var item in DatosCompartidos.ListaExperiencia)
                {
                    if (DatosCompartidos.ExperienciaAEditar == null || item.IdExperiencia != DatosCompartidos.ExperienciaAEditar.IdExperiencia)
                    {
                        listaExperienciaLocal.Add(item);
                    }
                }
            }

            // Filtrado de Habilidades
            if (DatosCompartidos.ListaHabilidades != null)
            {
                listaHabilidadesLocal = new List<Habilidad>();
                foreach (var item in DatosCompartidos.ListaHabilidades)
                {
                    if (DatosCompartidos.HabilidadAEditar == null || item.IdHabilidad != DatosCompartidos.HabilidadAEditar.IdHabilidad)
                    {
                        listaHabilidadesLocal.Add(item);
                    }
                }
            }

            // Filtrado de Referencias
            if (DatosCompartidos.ListaReferencias != null)
            {
                listaReferenciasLocal = new List<Referencia>();
                foreach (var item in DatosCompartidos.ListaReferencias)
                {
                    if (DatosCompartidos.ReferenciaAEditar == null || item.IdReferencia != DatosCompartidos.ReferenciaAEditar.IdReferencia)
                    {
                        listaReferenciasLocal.Add(item);
                    }
                }
            }
        }

        // --- SISTEMA DE BLOQUEO QUIRURGICO ---

        // Bloquea (deshabilita) TODOS los controles del formulario.
        // Se utiliza como paso previo antes de habilitar solo la seccion deseada.
        private void BloquearTodo()
        {
            // Datos Personales
            txtNombreCompleto.Enabled = false;
            txtTelefono.Enabled = false;
            txtCorreo.Enabled = false;
            boxDepartamento.Enabled = false;
            TxtObjetivo.Enabled = false;
            bttnImagen.Enabled = false;

            // Formacion Academica
            txtInstitucion.Enabled = false;
            txtTitulo.Enabled = false;
            txtUbicacion.Enabled = false;
            txtDiplomado.Enabled = false;
            dateFechaInicio.Enabled = false;
            dateFechaFinalizacion.Enabled = false;
            btnMas.Enabled = false;

            // Experiencia Profesional
            txtCargo.Enabled = false;
            txtEntidad.Enabled = false;
            dateEntrada.Enabled = false;
            dateSalida.Enabled = false;

            // Habilidades
            txtHabilidad.Enabled = false;
            txtCompetencia.Enabled = false;
            txtDominio.Enabled = false;

            // Referencias
            txtRefPersonalNombre.Enabled = false;
            txtRefPersonalTelefono.Enabled = false;
            txtRefLaboralNombre.Enabled = false;
            txtRefLaboralTelefono.Enabled = false;
        }

        // Habilita exclusivamente los controles correspondientes a Formacion Academica.
        private void DesbloquearSoloEstudios()
        {
            txtInstitucion.Enabled = true;
            txtTitulo.Enabled = true;
            txtUbicacion.Enabled = true;
            txtDiplomado.Enabled = true;
            dateFechaInicio.Enabled = true;
            dateFechaFinalizacion.Enabled = true;
            btnMas.Enabled = true;
        }

        // Habilita exclusivamente los controles correspondientes a Experiencia Profesional.
        private void DesbloquearSoloExperiencia()
        {
            txtCargo.Enabled = true;
            txtEntidad.Enabled = true;
            dateEntrada.Enabled = true;
            dateSalida.Enabled = true;
        }

        // Habilita exclusivamente los controles correspondientes a Habilidades.
        private void DesbloquearSoloHabilidades()
        {
            txtHabilidad.Enabled = true;
            txtCompetencia.Enabled = true;
            txtDominio.Enabled = true;
        }

        // Habilita exclusivamente los controles correspondientes a Referencias.
        private void DesbloquearSoloReferencias()
        {
            txtRefPersonalNombre.Enabled = true;
            txtRefPersonalTelefono.Enabled = true;
            txtRefLaboralNombre.Enabled = true;
            txtRefLaboralTelefono.Enabled = true;
        }

        // Metodo obsoleto conservado por compatibilidad. Ya no se usa activamente.
        private void BloquearDatosPersonales()
        {
            txtNombreCompleto.Enabled = false;
            txtTelefono.Enabled = false;
            txtCorreo.Enabled = false;
            boxDepartamento.Enabled = false;
            TxtObjetivo.Enabled = false;
            bttnImagen.Enabled = false;
        }

        // --- BOTONES DE AGREGAR A LISTAS TEMPORALES ---

        // 1. Agregar un registro de Estudio a la lista local.
        private void btnMas_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInstitucion.Text))
            {
                MessageBox.Show("La institución es obligatoria.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            listaEstudiosLocal.Add(new FormacionAcademica
            {
                Institucion = txtInstitucion.Text.Trim(),
                Titulo = txtTitulo.Text.Trim(),
                Ubicacion = txtUbicacion.Text.Trim(),
                FechaInicio = dateFechaInicio.Value,
                FechaFin = dateFechaFinalizacion.Value,
                TipoEstudio = string.IsNullOrWhiteSpace(txtDiplomado.Text) ? "Título Universitario" : txtDiplomado.Text.Trim()
            });

            MessageBox.Show("Estudio agregado a la lista.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LimpiarCamposEstudios();
        }

        // 2. Agregar un registro de Experiencia a la lista local.
        private void AgregarExperiencia_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCargo.Text))
            {
                MessageBox.Show("El cargo es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            listaExperienciaLocal.Add(new ExperienciaLaboral
            {
                Cargo = txtCargo.Text.Trim(),
                Entidad = txtEntidad.Text.Trim(),
                FechaInicio = dateEntrada.Value,
                FechaFin = dateSalida.Value
            });

            MessageBox.Show("Experiencia agregada a la lista.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LimpiarCamposExperiencia();
        }

        // 3. Agregar un registro de Habilidad a la lista local.
        private void AgregarHabilidad_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHabilidad.Text))
            {
                MessageBox.Show("El nombre de la habilidad es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            listaHabilidadesLocal.Add(new Habilidad
            {
                Nombre = txtHabilidad.Text.Trim(),
                Competencia = txtCompetencia.Text.Trim(),
                Dominio = txtDominio.Text.Trim()
            });

            MessageBox.Show("Habilidad agregada a la lista.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LimpiarCamposHabilidades();
        }

        // 4. Agregar una Referencia Personal a la lista local.
        private void AgregarReferenciaPersonal_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRefPersonalNombre.Text))
            {
                MessageBox.Show("El nombre de la referencia personal es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            listaReferenciasLocal.Add(new Referencia
            {
                Nombre = txtRefPersonalNombre.Text.Trim(),
                Telefono = txtRefPersonalTelefono.Text.Trim(),
                Tipo = "Personal"
            });

            MessageBox.Show("Referencia personal agregada a la lista.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LimpiarCamposReferenciaPersonal();
        }

        // 5. Agregar una Referencia Laboral a la lista local.
        private void AgregarReferenciaLaboral_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRefLaboralNombre.Text))
            {
                MessageBox.Show("El nombre de la referencia laboral es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            listaReferenciasLocal.Add(new Referencia
            {
                Nombre = txtRefLaboralNombre.Text.Trim(),
                Telefono = txtRefLaboralTelefono.Text.Trim(),
                Tipo = "Laboral"
            });

            MessageBox.Show("Referencia laboral agregada a la lista.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LimpiarCamposReferenciaLaboral();
        }

        // Metodo heredado para compatibilidad, redirige al usuario a los botones especificos.
        private void AgregarReferencia_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Use los botones específicos para agregar referencias:\n- Referencia Personal\n- Referencia Laboral",
                            "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- METODOS DE LIMPIEZA DE CAMPOS INDIVIDUALES ---

        private void LimpiarCamposEstudios()
        {
            txtInstitucion.Clear();
            txtTitulo.Clear();
            txtUbicacion.Clear();
            txtDiplomado.Clear();
        }

        private void LimpiarCamposExperiencia()
        {
            txtCargo.Clear();
            txtEntidad.Clear();
        }

        private void LimpiarCamposHabilidades()
        {
            txtHabilidad.Clear();
            txtCompetencia.Clear();
            txtDominio.Clear();
        }

        private void LimpiarCamposReferenciaPersonal()
        {
            txtRefPersonalNombre.Clear();
            txtRefPersonalTelefono.Clear();
        }

        private void LimpiarCamposReferenciaLaboral()
        {
            txtRefLaboralNombre.Clear();
            txtRefLaboralTelefono.Clear();
        }

        // Manejador para cargar una imagen desde el sistema de archivos.
        private void bttnImagen_Click(object sender, EventArgs e)
        {
            if (!bttnImagen.Enabled) return;

            try
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp",
                    Title = "Seleccionar foto de perfil"
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    fotoBytes = File.ReadAllBytes(ofd.FileName);
                    MessageBox.Show("Imagen cargada correctamente.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la imagen: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- BOTON GUARDAR (LOGICA PRINCIPAL) ---
        // Maneja la persistencia de datos, decidiendo si guardar todo el CV o actualizar un solo registro.
        private void btnGuardar_Click(object sender, EventArgs e)
        {
            // Validacion condicional: El nombre solo es obligatorio si el campo esta habilitado.
            if (txtNombreCompleto.Enabled && string.IsNullOrWhiteSpace(txtNombreCompleto.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validacion para edicion especifica: Asegura que al menos un campo obligatorio tenga datos.
            if (!EsModoEdicionCompleta())
            {
                if (!ValidarCamposEnEdicionEspecifica())
                {
                    MessageBox.Show("Debe completar al menos un campo obligatorio para guardar los cambios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                // 1. Determinar el ID del usuario objetivo (Seleccionado por Admin o Usuario Actual).
                int targetId = DatosCompartidos.IdUsuarioSeleccionado != 0
                    ? DatosCompartidos.IdUsuarioSeleccionado
                    : SesionActual.IdUsuario;

                if (targetId == 0)
                {
                    MessageBox.Show("Error: No se pudo identificar el usuario.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 2. Establecer conexion con la base de datos y comenzar transaccion.
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null)
                    {
                        MessageBox.Show("No se pudo conectar a la base de datos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    SqlTransaction tx = con.BeginTransaction();

                    try
                    {
                        // 3. Decidir la accion a tomar segun el modo de operacion.
                        if (EsModoEdicionCompleta())
                        {
                            // MODO COMPLETO: Guarda o actualiza todo el CV (Datos personales + todas las listas).
                            GuardarCVCompleto(con, tx, targetId);
                        }
                        else
                        {
                            // MODO ESPECIFICO: Actualiza unicamente el registro que se esta editando.
                            GuardarItemEspecifico(con, tx, targetId);
                        }

                        // Confirmar transaccion.
                        tx.Commit();
                        MessageBox.Show("CV guardado correctamente.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Navegacion de retorno despues del guardado exitoso.
                        NavegacionDespuesDeGuardar();
                    }
                    catch (Exception ex)
                    {
                        // Revertir cambios en caso de error.
                        tx.Rollback();
                        MessageBox.Show($"Error al guardar: {ex.Message}\n\nDetalles: {ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error general: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Valida que los campos requeridos no esten vacios cuando se esta en modo de edicion especifica.
        private bool ValidarCamposEnEdicionEspecifica()
        {
            // Validacion para Estudios
            if (DatosCompartidos.EstudioAEditar != null)
            {
                return !string.IsNullOrWhiteSpace(txtInstitucion.Text);
            }

            // Validacion para Experiencia
            if (DatosCompartidos.ExperienciaAEditar != null)
            {
                return !string.IsNullOrWhiteSpace(txtCargo.Text);
            }

            // Validacion para Habilidades
            if (DatosCompartidos.HabilidadAEditar != null)
            {
                return !string.IsNullOrWhiteSpace(txtHabilidad.Text);
            }

            // Validacion para Referencias (Personal o Laboral)
            if (DatosCompartidos.ReferenciaAEditar != null)
            {
                return !string.IsNullOrWhiteSpace(txtRefPersonalNombre.Text) ||
                       !string.IsNullOrWhiteSpace(txtRefLaboralNombre.Text);
            }

            return true; // Por defecto permitir guardar si no hay condicion especifica.
        }

        // Gestiona la transicion de pantallas despues de guardar los datos.
        // Limpia la memoria compartida y redirige al formulario de visualizacion (VerCV).
        private void NavegacionDespuesDeGuardar()
        {
            try
            {
                // Se preserva el ID del usuario antes de limpiar los datos compartidos.
                int usuarioIdObjetivo = DatosCompartidos.IdUsuarioSeleccionado != 0
                    ? DatosCompartidos.IdUsuarioSeleccionado
                    : SesionActual.IdUsuario;

                // Limpieza de memoria compartida.
                DatosCompartidos.Limpiar();

                // Uso de Invoke para asegurar que la manipulacion de la UI ocurra en el hilo principal.
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => CambiarAVerCV(usuarioIdObjetivo)));
                }
                else
                {
                    CambiarAVerCV(usuarioIdObjetivo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en navegación: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
            }
        }

        // Metodo auxiliar para instanciar y mostrar el formulario VerCV de forma segura.
        private void CambiarAVerCV(int usuarioId)
        {
            try
            {
                // Se establece el ID del usuario que sera visualizado.
                DatosCompartidos.IdUsuarioSeleccionado = usuarioId;

                VerCV nuevoVerCV = new VerCV();
                nuevoVerCV.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir VerCV: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        // Realiza el guardado completo del CV. Incluye UPSERT de datos personales e insercion masiva de listas.
        // Tambien captura automaticamente los datos que quedaron en los campos de texto sin agregar a las listas.
        private void GuardarCVCompleto(SqlConnection con, SqlTransaction tx, int targetId)
        {
            // A. UPSERT (Actualizar o Insertar) Datos Personales.
            if (txtNombreCompleto.Enabled)
            {
                string checkQuery = "SELECT COUNT(*) FROM DatosPersonales WHERE IdUsuario = @Id";
                SqlCommand checkCmd = new SqlCommand(checkQuery, con, tx);
                checkCmd.Parameters.AddWithValue("@Id", targetId);
                int count = (int)checkCmd.ExecuteScalar();

                string sqlPers = count > 0
                    ? "UPDATE DatosPersonales SET Nombre=@N, Telefono=@T, Correo=@C, Departamento=@D, Objetivo=@O, Foto=@F WHERE IdUsuario=@Id"
                    : "INSERT INTO DatosPersonales (IdUsuario, Nombre, Telefono, Correo, Departamento, Objetivo, Foto) VALUES (@Id, @N, @T, @C, @D, @O, @F)";

                using (SqlCommand cmd = new SqlCommand(sqlPers, con, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", targetId);
                    cmd.Parameters.AddWithValue("@N", txtNombreCompleto.Text.Trim());
                    cmd.Parameters.AddWithValue("@T", txtTelefono.Text.Trim());
                    cmd.Parameters.AddWithValue("@C", txtCorreo.Text.Trim());
                    cmd.Parameters.AddWithValue("@D", boxDepartamento.Text.Trim());
                    cmd.Parameters.AddWithValue("@O", TxtObjetivo.Text.Trim());

                    SqlParameter pFoto = new SqlParameter("@F", SqlDbType.VarBinary);
                    pFoto.Value = (fotoBytes != null) ? (object)fotoBytes : DBNull.Value;
                    cmd.Parameters.Add(pFoto);

                    cmd.ExecuteNonQuery();
                }
            }

            // B. Auto-captura de datos ingresados en los campos de texto pero no agregados a las listas (UX Friendly).

            // Auto-captura de Estudios
            if (txtInstitucion.Enabled && !string.IsNullOrWhiteSpace(txtInstitucion.Text))
            {
                listaEstudiosLocal.Add(new FormacionAcademica
                {
                    Institucion = txtInstitucion.Text.Trim(),
                    Titulo = txtTitulo.Text.Trim(),
                    Ubicacion = txtUbicacion.Text.Trim(),
                    FechaInicio = dateFechaInicio.Value,
                    FechaFin = dateFechaFinalizacion.Value,
                    TipoEstudio = string.IsNullOrWhiteSpace(txtDiplomado.Text) ? "Título Universitario" : txtDiplomado.Text.Trim()
                });
            }

            // Auto-captura de Experiencia
            if (txtCargo.Enabled && !string.IsNullOrWhiteSpace(txtCargo.Text))
            {
                listaExperienciaLocal.Add(new ExperienciaLaboral
                {
                    Cargo = txtCargo.Text.Trim(),
                    Entidad = txtEntidad.Text.Trim(),
                    FechaInicio = dateEntrada.Value,
                    FechaFin = dateSalida.Value
                });
            }

            // Auto-captura de Habilidades
            if (txtHabilidad.Enabled && !string.IsNullOrWhiteSpace(txtHabilidad.Text))
            {
                listaHabilidadesLocal.Add(new Habilidad
                {
                    Nombre = txtHabilidad.Text.Trim(),
                    Competencia = txtCompetencia.Text.Trim(),
                    Dominio = txtDominio.Text.Trim()
                });
            }

            // Auto-captura de Referencias (Personal y Laboral)
            if (txtRefPersonalNombre.Enabled && !string.IsNullOrWhiteSpace(txtRefPersonalNombre.Text))
            {
                listaReferenciasLocal.Add(new Referencia
                {
                    Nombre = txtRefPersonalNombre.Text.Trim(),
                    Telefono = txtRefPersonalTelefono.Text.Trim(),
                    Tipo = "Personal"
                });
            }

            if (txtRefLaboralNombre.Enabled && !string.IsNullOrWhiteSpace(txtRefLaboralNombre.Text))
            {
                listaReferenciasLocal.Add(new Referencia
                {
                    Nombre = txtRefLaboralNombre.Text.Trim(),
                    Telefono = txtRefLaboralTelefono.Text.Trim(),
                    Tipo = "Laboral"
                });
            }

            // C. Limpieza previa de tablas hijas para evitar duplicados (Borrar e Insertar de nuevo).
            EjecutarDelete(con, tx, "FormacionAcademica", targetId);
            EjecutarDelete(con, tx, "ExperienciaLaboral", targetId);
            EjecutarDelete(con, tx, "Habilidades", targetId);
            EjecutarDelete(con, tx, "Referencias", targetId);

            // D. Insercion masiva de todas las listas.
            InsertarEstudios(con, tx, targetId);
            InsertarExperiencias(con, tx, targetId);
            InsertarHabilidades(con, tx, targetId);
            InsertarReferencias(con, tx, targetId);
        }

        // Realiza la actualizacion de un unico registro especifico en la base de datos.
        // Utiliza SQL dinamico para actualizar solo los campos que tienen informacion.
        private void GuardarItemEspecifico(SqlConnection con, SqlTransaction tx, int targetId)
        {
            // Logica especifica para actualizacion de Estudios.
            if (DatosCompartidos.EstudioAEditar != null && txtInstitucion.Enabled)
            {
                List<string> camposActualizar = new List<string>();
                List<SqlParameter> parametros = new List<SqlParameter>();

                parametros.Add(new SqlParameter("@Id", DatosCompartidos.EstudioAEditar.IdFormacion));

                if (!string.IsNullOrWhiteSpace(txtInstitucion.Text))
                {
                    camposActualizar.Add("Institucion=@Inst");
                    parametros.Add(new SqlParameter("@Inst", txtInstitucion.Text.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(txtTitulo.Text))
                {
                    camposActualizar.Add("Titulo=@Tit");
                    parametros.Add(new SqlParameter("@Tit", txtTitulo.Text.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(txtUbicacion.Text))
                {
                    camposActualizar.Add("Ubicacion=@Ubi");
                    parametros.Add(new SqlParameter("@Ubi", txtUbicacion.Text.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(txtDiplomado.Text))
                {
                    camposActualizar.Add("TipoEstudio=@Tipo");
                    parametros.Add(new SqlParameter("@Tipo", txtDiplomado.Text.Trim()));
                }

                // Las fechas siempre se actualizan.
                camposActualizar.Add("FechaInicio=@Ini");
                camposActualizar.Add("FechaFin=@Fin");
                parametros.Add(new SqlParameter("@Ini", dateFechaInicio.Value));
                parametros.Add(new SqlParameter("@Fin", dateFechaFinalizacion.Value));

                if (camposActualizar.Count > 2)
                {
                    string sql = $"UPDATE FormacionAcademica SET {string.Join(", ", camposActualizar)} WHERE IdFormacion=@Id";
                    using (SqlCommand cmd = new SqlCommand(sql, con, tx))
                    {
                        foreach (var param in parametros)
                            cmd.Parameters.Add(param);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            // Logica especifica para actualizacion de Experiencia.
            else if (DatosCompartidos.ExperienciaAEditar != null && txtCargo.Enabled)
            {
                List<string> camposActualizar = new List<string>();
                List<SqlParameter> parametros = new List<SqlParameter>();

                parametros.Add(new SqlParameter("@Id", DatosCompartidos.ExperienciaAEditar.IdExperiencia));

                if (!string.IsNullOrWhiteSpace(txtCargo.Text))
                {
                    camposActualizar.Add("Cargo=@Car");
                    parametros.Add(new SqlParameter("@Car", txtCargo.Text.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(txtEntidad.Text))
                {
                    camposActualizar.Add("Empresa=@Emp");
                    parametros.Add(new SqlParameter("@Emp", txtEntidad.Text.Trim()));
                }

                camposActualizar.Add("FechaInicio=@Ini");
                camposActualizar.Add("FechaFin=@Fin");
                parametros.Add(new SqlParameter("@Ini", dateEntrada.Value));
                parametros.Add(new SqlParameter("@Fin", dateSalida.Value));

                if (camposActualizar.Count > 2)
                {
                    string sql = $"UPDATE ExperienciaLaboral SET {string.Join(", ", camposActualizar)} WHERE IdExperiencia=@Id";
                    using (SqlCommand cmd = new SqlCommand(sql, con, tx))
                    {
                        foreach (var param in parametros)
                            cmd.Parameters.Add(param);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            // Logica especifica para actualizacion de Habilidades.
            else if (DatosCompartidos.HabilidadAEditar != null && txtHabilidad.Enabled)
            {
                List<string> camposActualizar = new List<string>();
                List<SqlParameter> parametros = new List<SqlParameter>();

                parametros.Add(new SqlParameter("@Id", DatosCompartidos.HabilidadAEditar.IdHabilidad));

                if (!string.IsNullOrWhiteSpace(txtHabilidad.Text))
                {
                    camposActualizar.Add("Habilidad=@Hab");
                    parametros.Add(new SqlParameter("@Hab", txtHabilidad.Text.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(txtCompetencia.Text))
                {
                    camposActualizar.Add("Competencia=@Com");
                    parametros.Add(new SqlParameter("@Com", txtCompetencia.Text.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(txtDominio.Text))
                {
                    camposActualizar.Add("Dominio=@Dom");
                    parametros.Add(new SqlParameter("@Dom", txtDominio.Text.Trim()));
                }

                if (camposActualizar.Count > 0)
                {
                    string sql = $"UPDATE Habilidades SET {string.Join(", ", camposActualizar)} WHERE IdHabilidad=@Id";
                    using (SqlCommand cmd = new SqlCommand(sql, con, tx))
                    {
                        foreach (var param in parametros)
                            cmd.Parameters.Add(param);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            // Logica especifica para actualizacion de Referencias.
            else if (DatosCompartidos.ReferenciaAEditar != null)
            {
                List<string> camposActualizar = new List<string>();
                List<SqlParameter> parametros = new List<SqlParameter>();

                parametros.Add(new SqlParameter("@Id", DatosCompartidos.ReferenciaAEditar.IdReferencia));

                string nuevoNombre = "";
                string nuevoTelefono = "";

                // Se determinan los valores segun el tipo de referencia que se esta editando.
                if (DatosCompartidos.ReferenciaAEditar.Tipo == "Personal" ||
                    (!string.IsNullOrWhiteSpace(txtRefPersonalNombre.Text) && txtRefPersonalNombre.Enabled))
                {
                    nuevoNombre = txtRefPersonalNombre.Text;
                    nuevoTelefono = txtRefPersonalTelefono.Text;
                }
                else if (DatosCompartidos.ReferenciaAEditar.Tipo == "Laboral" ||
                         (!string.IsNullOrWhiteSpace(txtRefLaboralNombre.Text) && txtRefLaboralNombre.Enabled))
                {
                    nuevoNombre = txtRefLaboralNombre.Text;
                    nuevoTelefono = txtRefLaboralTelefono.Text;
                }

                if (!string.IsNullOrWhiteSpace(nuevoNombre))
                {
                    camposActualizar.Add("Nombre=@Nom");
                    parametros.Add(new SqlParameter("@Nom", nuevoNombre.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(nuevoTelefono))
                {
                    camposActualizar.Add("Telefono=@Tel");
                    parametros.Add(new SqlParameter("@Tel", nuevoTelefono.Trim()));
                }

                if (camposActualizar.Count > 0)
                {
                    string sql = $"UPDATE Referencias SET {string.Join(", ", camposActualizar)} WHERE IdReferencia=@Id";
                    using (SqlCommand cmd = new SqlCommand(sql, con, tx))
                    {
                        foreach (var param in parametros)
                            cmd.Parameters.Add(param);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // --- METODOS DE INSERCION DE DATOS EN BD ---

        private void InsertarEstudios(SqlConnection con, SqlTransaction tx, int targetId)
        {
            foreach (var item in listaEstudiosLocal)
            {
                string sql = "INSERT INTO FormacionAcademica (IdUsuario, Institucion, Titulo, Ubicacion, FechaInicio, FechaFin, TipoEstudio) " +
                             "VALUES (@Id, @Inst, @Tit, @Ubi, @Ini, @Fin, @Tipo)";
                using (SqlCommand cmd = new SqlCommand(sql, con, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", targetId);
                    cmd.Parameters.AddWithValue("@Inst", item.Institucion ?? "");
                    cmd.Parameters.AddWithValue("@Tit", item.Titulo ?? "");
                    cmd.Parameters.AddWithValue("@Ubi", item.Ubicacion ?? "");
                    cmd.Parameters.AddWithValue("@Ini", item.FechaInicio);
                    cmd.Parameters.AddWithValue("@Fin", item.FechaFin);
                    cmd.Parameters.AddWithValue("@Tipo", item.TipoEstudio ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InsertarExperiencias(SqlConnection con, SqlTransaction tx, int targetId)
        {
            foreach (var item in listaExperienciaLocal)
            {
                string sql = "INSERT INTO ExperienciaLaboral (IdUsuario, Cargo, Empresa, FechaInicio, FechaFin) " +
                             "VALUES (@Id, @Car, @Emp, @Ini, @Fin)";
                using (SqlCommand cmd = new SqlCommand(sql, con, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", targetId);
                    cmd.Parameters.AddWithValue("@Car", item.Cargo ?? "");
                    cmd.Parameters.AddWithValue("@Emp", item.Entidad ?? "");
                    cmd.Parameters.AddWithValue("@Ini", item.FechaInicio);
                    cmd.Parameters.AddWithValue("@Fin", item.FechaFin);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InsertarHabilidades(SqlConnection con, SqlTransaction tx, int targetId)
        {
            foreach (var item in listaHabilidadesLocal)
            {
                string sql = "INSERT INTO Habilidades (IdUsuario, Habilidad, Competencia, Dominio) " +
                             "VALUES (@Id, @Hab, @Com, @Dom)";
                using (SqlCommand cmd = new SqlCommand(sql, con, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", targetId);
                    cmd.Parameters.AddWithValue("@Hab", item.Nombre ?? "");
                    cmd.Parameters.AddWithValue("@Com", item.Competencia ?? "");
                    cmd.Parameters.AddWithValue("@Dom", item.Dominio ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InsertarReferencias(SqlConnection con, SqlTransaction tx, int targetId)
        {
            foreach (var item in listaReferenciasLocal)
            {
                string sql = "INSERT INTO Referencias (IdUsuario, Nombre, Telefono, Tipo) " +
                             "VALUES (@Id, @Nom, @Tel, @Tip)";
                using (SqlCommand cmd = new SqlCommand(sql, con, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", targetId);
                    cmd.Parameters.AddWithValue("@Nom", item.Nombre ?? "");
                    cmd.Parameters.AddWithValue("@Tel", item.Telefono ?? "");
                    cmd.Parameters.AddWithValue("@Tip", item.Tipo ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Metodo de seguridad para eliminar registros antes de una reinsercion masiva.
        private void EjecutarDelete(SqlConnection con, SqlTransaction tx, string tabla, int idUsuario)
        {
            string query = $"DELETE FROM {tabla} WHERE IdUsuario = @IdUsr";
            using (SqlCommand cmd = new SqlCommand(query, con, tx))
            {
                cmd.Parameters.AddWithValue("@IdUsr", idUsuario);
                cmd.ExecuteNonQuery();
            }
        }

        // --- NAVEGACION Y EVENTOS GENERALES ---

        // Navegacion a la vista de Empleados (Solo Admin).
        private void btnEmpleado_Click(object sender, EventArgs e)
        {
            if (SesionActual.Rol != "Admin")
            {
                MessageBox.Show("Acceso denegado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            new Empleado(this).Show();
            this.Hide();

        }

        private void buttonClose_Click(object sender, EventArgs e)
        {

        }

        // Cierre de sesion y retorno al Login.
        private void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            DatosCompartidos.Limpiar();
            SesionActual.CerrarSesion();
            new Login().Show();
            this.Hide();
        }

        // Navegacion a la vista de visualizacion de CV.
        private void btnVerCV_Click(object sender, EventArgs e)
        {
            VerCV vercv = new VerCV();
            vercv.Show();
            this.Hide();
        }

        // Eventos placeholder sin implementacion actual.
        private void txtRefPersonalNombre_TextContentChanged(object sender, EventArgs e) { }
        private void txtRefPersonalTelefono_TextContentChanged(object sender, EventArgs e) { }
        private void txtRefLaboralNombre_TextContentChanged(object sender, EventArgs e) { }
        private void txtRefLaboralTelefono_TextContentChanged(object sender, EventArgs e) { }
        private void btnCrearCV_Click(object sender, EventArgs e) { }

        private void txtHabilidad_TextContentChanged(object sender, EventArgs e) { }
        private void txtCompetencia_TextContentChanged(object sender, EventArgs e) { }
        private void txtDominio_TextContentChanged(object sender, EventArgs e) { }
    }
}