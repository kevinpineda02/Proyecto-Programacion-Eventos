using GestorRRHH.Base_de_datos;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using SystemDrawing = System.Drawing; // Alias para evitar conflictos de nombres entre bibliotecas graficas

namespace GestorRRHH.PDF
{
    // Clase principal encargada de la generacion de documentos PDF para los curriculums.
    // Utiliza la libreria iTextSharp para la maquetacion y exportacion.
    public class GeneradorCVPDF
    {
        // Configuracion de colores corporativos o esteticos para el documento.
        // Se definen como campos de lectura para su reutilizacion en todo el reporte.
        private readonly BaseColor ColorPrimario = new BaseColor(41, 128, 185);     // Azul profesional
        private readonly BaseColor ColorSecundario = new BaseColor(52, 73, 94);     // Gris oscuro
        private readonly BaseColor ColorTexto = new BaseColor(44, 62, 80);          // Gris texto principal
        private readonly BaseColor ColorFondo = new BaseColor(248, 249, 250);       // Fondo gris muy claro (opcional)

        // Definicion de objetos de fuente para los diferentes estilos de texto (titulos, subtitulos, etc).
        private iTextSharp.text.Font fuenteTitulo;
        private iTextSharp.text.Font fuenteSubtitulo;
        private iTextSharp.text.Font fuenteTexto;
        private iTextSharp.text.Font fuenteNegrita;
        private iTextSharp.text.Font fuentePequeña;

        // Constructor de la clase. Inicializa las configuraciones necesarias como las fuentes.
        public GeneradorCVPDF()
        {
            ConfigurarFuentes();
        }

        // Metodo auxiliar para instanciar las fuentes que se usaran en el PDF.
        // Incluye un manejo de errores basico para asegurar que siempre haya una fuente disponible.
        private void ConfigurarFuentes()
        {
            try
            {
                // Intenta cargar fuentes estandar Helvetica.
                fuenteTitulo = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 24f, iTextSharp.text.Font.BOLD, ColorPrimario);
                fuenteSubtitulo = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 16f, iTextSharp.text.Font.BOLD, ColorSecundario);
                fuenteTexto = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 11f, iTextSharp.text.Font.NORMAL, ColorTexto);
                fuenteNegrita = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 11f, iTextSharp.text.Font.BOLD, ColorSecundario);
                fuentePequeña = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10f, iTextSharp.text.Font.NORMAL, ColorTexto);
            }
            catch
            {
                // Si ocurre un error cargando Helvetica, se utiliza Times Roman como respaldo.
                fuenteTitulo = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.TIMES_ROMAN, 24f, iTextSharp.text.Font.BOLD);
                fuenteSubtitulo = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.TIMES_ROMAN, 16f, iTextSharp.text.Font.BOLD);
                fuenteTexto = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.TIMES_ROMAN, 11f, iTextSharp.text.Font.NORMAL);
                fuenteNegrita = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.TIMES_ROMAN, 11f, iTextSharp.text.Font.BOLD);
                fuentePequeña = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.TIMES_ROMAN, 10f, iTextSharp.text.Font.NORMAL);
            }
        }

        // Metodo principal que orquesta todo el proceso de creacion del CV.
        // Recibe el ID del usuario y opcionalmente la ruta donde guardar el archivo.
        public bool GenerarCV(int idUsuario, string rutaDestino = null)
        {
            try
            {
                // Paso 1: Recuperar toda la informacion del usuario desde la base de datos.
                var datosCV = CargarDatosCompletos(idUsuario);

                // Validacion basica: si no hay datos, se detiene el proceso.
                if (datosCV == null)
                {
                    MessageBox.Show("No se pudieron cargar los datos del usuario.", "Error",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Paso 2: Si no se especifico una ruta, se solicita al usuario que elija donde guardar.
                if (string.IsNullOrEmpty(rutaDestino))
                {
                    rutaDestino = SeleccionarRutaDestino(datosCV.Nombre);
                    // Si el usuario cancela la ventana de guardado, se aborta la operacion.
                    if (string.IsNullOrEmpty(rutaDestino))
                        return false;
                }

                // Paso 3: Inicializacion del documento PDF y el escritor de archivos.
                // Se definen los margenes (Izquierda, Derecha, Arriba, Abajo).
                Document documento = new Document(PageSize.A4, 40, 40, 50, 50);
                PdfWriter escritor = PdfWriter.GetInstance(documento, new FileStream(rutaDestino, FileMode.Create));

                // Se abre el documento para comenzar a escribir contenido.
                documento.Open();

                // Paso 4: Llamadas secuenciales a los metodos que construyen cada seccion del CV.
                GenerarEncabezado(documento, datosCV);
                GenerarObjetivo(documento, datosCV.ObjetivoProfesional);
                GenerarFormacionAcademica(documento, datosCV.Estudios);
                GenerarExperienciaLaboral(documento, datosCV.Experiencia);
                GenerarHabilidades(documento, datosCV.Habilidades);
                GenerarReferencias(documento, datosCV.Referencias);
                GenerarPiePagina(documento);

                // Se cierra el documento para finalizar la escritura y liberar el archivo.
                documento.Close();

                // Notificacion de exito al usuario.
                MessageBox.Show($"CV generado exitosamente en:\n{rutaDestino}", "PDF Generado",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Paso 5: Opcion para abrir el archivo generado inmediatamente.
                if (MessageBox.Show("¿Desea abrir el PDF generado?", "Abrir PDF",
                                  MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(rutaDestino);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Manejo general de excepciones durante la generacion.
                MessageBox.Show($"Error al generar PDF: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Metodo que centraliza la carga de datos.
        // Realiza multiples consultas a la base de datos para llenar el objeto DatosCV.
        private DatosCV CargarDatosCompletos(int idUsuario)
        {
            try
            {
                // Se establece la conexion con la base de datos.
                using (SqlConnection con = ConexionBD.ObtenerConexion())
                {
                    if (con == null) return null;

                    var datos = new DatosCV();

                    // Se invocan los metodos individuales de carga pasando la conexion abierta.
                    CargarDatosPersonalesPDF(con, idUsuario, datos);
                    datos.Estudios = CargarEstudiosPDF(con, idUsuario);
                    datos.Experiencia = CargarExperienciaPDF(con, idUsuario);
                    datos.Habilidades = CargarHabilidadesPDF(con, idUsuario);
                    datos.Referencias = CargarReferenciasPDF(con, idUsuario);

                    return datos;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // Consulta la tabla de Usuarios y DatosPersonales para obtener informacion basica y foto.
        private void CargarDatosPersonalesPDF(SqlConnection con, int idUsuario, DatosCV datos)
        {
            string query = @"
                SELECT 
                    ISNULL(D.Nombre, U.Login) AS Nombre,
                    ISNULL(D.Telefono, '') AS Telefono,
                    ISNULL(D.Correo, '') AS Correo,
                    ISNULL(D.Departamento, '') AS Departamento,
                    ISNULL(D.Objetivo, '') AS Objetivo,
                    D.Foto
                FROM dbo.Usuarios U
                LEFT JOIN dbo.DatosPersonales D ON U.IdUsuario = D.IdUsuario
                WHERE U.IdUsuario = @Id";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Mapeo de columnas a propiedades del objeto.
                        datos.Nombre = reader["Nombre"].ToString();
                        datos.Telefono = reader["Telefono"].ToString();
                        datos.Correo = reader["Correo"].ToString();
                        datos.Departamento = reader["Departamento"].ToString();
                        datos.ObjetivoProfesional = reader["Objetivo"].ToString();

                        // Manejo especifico para el campo binario de la foto.
                        if (reader["Foto"] != DBNull.Value)
                        {
                            datos.Foto = (byte[])reader["Foto"];
                        }
                    }
                }
            }
        }

        // Consulta y devuelve la lista de formacion academica ordenada por fecha.
        private List<FormacionAcademica> CargarEstudiosPDF(SqlConnection con, int idUsuario)
        {
            var estudios = new List<FormacionAcademica>();
            string query = @"
                SELECT Institucion, Titulo, Ubicacion, FechaInicio, FechaFin, TipoEstudio
                FROM FormacionAcademica 
                WHERE IdUsuario = @Id
                ORDER BY FechaInicio DESC";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        estudios.Add(new FormacionAcademica
                        {
                            Institucion = reader["Institucion"].ToString(),
                            Titulo = reader["Titulo"].ToString(),
                            Ubicacion = reader["Ubicacion"].ToString(),
                            FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                            FechaFin = Convert.ToDateTime(reader["FechaFin"]),
                            TipoEstudio = reader["TipoEstudio"].ToString()
                        });
                    }
                }
            }
            return estudios;
        }

        // Consulta y devuelve la lista de experiencia laboral.
        private List<ExperienciaLaboral> CargarExperienciaPDF(SqlConnection con, int idUsuario)
        {
            var experiencia = new List<ExperienciaLaboral>();
            string query = @"
                SELECT Cargo, Empresa, FechaInicio, FechaFin
                FROM ExperienciaLaboral 
                WHERE IdUsuario = @Id
                ORDER BY FechaInicio DESC";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        experiencia.Add(new ExperienciaLaboral
                        {
                            Cargo = reader["Cargo"].ToString(),
                            Entidad = reader["Empresa"].ToString(),
                            FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                            FechaFin = Convert.ToDateTime(reader["FechaFin"])
                        });
                    }
                }
            }
            return experiencia;
        }

        // Consulta y devuelve la lista de habilidades.
        private List<Habilidad> CargarHabilidadesPDF(SqlConnection con, int idUsuario)
        {
            var habilidades = new List<Habilidad>();
            string query = @"
                SELECT Habilidad, Competencia, Dominio
                FROM Habilidades 
                WHERE IdUsuario = @Id
                ORDER BY Habilidad";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        habilidades.Add(new Habilidad
                        {
                            Nombre = reader["Habilidad"].ToString(),
                            Competencia = reader["Competencia"].ToString(),
                            Dominio = reader["Dominio"].ToString()
                        });
                    }
                }
            }
            return habilidades;
        }

        // Consulta y devuelve la lista de referencias personales o laborales.
        private List<Referencia> CargarReferenciasPDF(SqlConnection con, int idUsuario)
        {
            var referencias = new List<Referencia>();
            string query = @"
                SELECT Nombre, Telefono, Tipo
                FROM Referencias 
                WHERE IdUsuario = @Id
                ORDER BY Tipo, Nombre";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        referencias.Add(new Referencia
                        {
                            Nombre = reader["Nombre"].ToString(),
                            Telefono = reader["Telefono"].ToString(),
                            Tipo = reader["Tipo"].ToString()
                        });
                    }
                }
            }
            return referencias;
        }

        // Genera la seccion superior del documento.
        // Utiliza una tabla sin bordes para alinear la foto a la izquierda y el texto a la derecha.
        private void GenerarEncabezado(Document documento, DatosCV datos)
        {
            // Creacion de tabla de 2 columnas con anchos relativos (20% y 80%).
            PdfPTable tablaEncabezado = new PdfPTable(2);
            tablaEncabezado.WidthPercentage = 100;
            tablaEncabezado.SetWidths(new float[] { 20f, 80f });

            // Configuracion de la celda de la fotografia.
            PdfPCell celdaFoto = new PdfPCell();
            celdaFoto.Border = Rectangle.NO_BORDER;
            celdaFoto.HorizontalAlignment = Element.ALIGN_CENTER;
            celdaFoto.VerticalAlignment = Element.ALIGN_MIDDLE;

            // Logica para insertar la imagen si existe.
            if (datos.Foto != null)
            {
                try
                {
                    iTextSharp.text.Image foto = iTextSharp.text.Image.GetInstance(datos.Foto);
                    foto.ScaleToFit(100f, 120f); // Redimensionamiento para ajustar al diseño
                    celdaFoto.AddElement(foto);
                }
                catch
                {
                    // Si la imagen esta corrupta, se muestra texto alternativo.
                    celdaFoto.AddElement(new Phrase("FOTO", fuenteTexto));
                }
            }
            else
            {
                celdaFoto.AddElement(new Phrase("Sin foto", fuentePequeña));
            }

            // Configuracion de la celda de informacion textual.
            PdfPCell celdaDatos = new PdfPCell();
            celdaDatos.Border = Rectangle.NO_BORDER;
            celdaDatos.PaddingLeft = 20f;

            // Agregado del Nombre.
            Paragraph nombre = new Paragraph(datos.Nombre.ToUpper(), fuenteTitulo);
            nombre.SpacingAfter = 5f;
            celdaDatos.AddElement(nombre);

            // Agregado del Departamento si existe.
            if (!string.IsNullOrEmpty(datos.Departamento))
            {
                Paragraph depto = new Paragraph(datos.Departamento, fuenteSubtitulo);
                depto.SpacingAfter = 15f;
                celdaDatos.AddElement(depto);
            }

            // Agregado de datos de contacto (Telefono y Correo).
            if (!string.IsNullOrEmpty(datos.Telefono))
            {
                Paragraph tel = new Paragraph($"Telefono: {datos.Telefono}", fuenteTexto);
                tel.SpacingAfter = 5f;
                celdaDatos.AddElement(tel);
            }

            if (!string.IsNullOrEmpty(datos.Correo))
            {
                Paragraph email = new Paragraph($"Email: {datos.Correo}", fuenteTexto);
                email.SpacingAfter = 5f;
                celdaDatos.AddElement(email);
            }

            // Se añaden las celdas a la tabla y la tabla al documento.
            tablaEncabezado.AddCell(celdaFoto);
            tablaEncabezado.AddCell(celdaDatos);

            documento.Add(tablaEncabezado);

            // Se dibuja una linea separadora debajo del encabezado.
            documento.Add(new Paragraph(" ", fuenteTexto));
            iTextSharp.text.pdf.draw.LineSeparator linea = new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, ColorPrimario, Element.ALIGN_CENTER, -2);
            documento.Add(new Chunk(linea));
            documento.Add(new Paragraph(" ", fuenteTexto));
        }

        // Genera el parrafo del objetivo profesional justificado.
        private void GenerarObjetivo(Document documento, string objetivo)
        {
            if (!string.IsNullOrEmpty(objetivo))
            {
                Paragraph titulo = new Paragraph("OBJETIVO PROFESIONAL", fuenteSubtitulo);
                titulo.SpacingAfter = 10f;
                documento.Add(titulo);

                Paragraph texto = new Paragraph(objetivo, fuenteTexto);
                texto.Alignment = Element.ALIGN_JUSTIFIED;
                texto.SpacingAfter = 20f;
                documento.Add(texto);
            }
        }

        // Itera sobre la lista de estudios y los agrega al documento.
        private void GenerarFormacionAcademica(Document documento, List<FormacionAcademica> estudios)
        {
            if (estudios == null || estudios.Count == 0) return;

            Paragraph titulo = new Paragraph("FORMACION ACADEMICA", fuenteSubtitulo);
            titulo.SpacingAfter = 15f;
            documento.Add(titulo);

            foreach (var estudio in estudios)
            {
                // Formato: Titulo (FechaInicio - FechaFin)
                string fechas = $"{estudio.FechaInicio:MM/yyyy} - {estudio.FechaFin:MM/yyyy}";
                Paragraph tituloEstudio = new Paragraph($"{estudio.Titulo} ({fechas})", fuenteNegrita);
                tituloEstudio.SpacingAfter = 5f;
                documento.Add(tituloEstudio);

                // Institucion y Ubicacion
                string institucion = $"{estudio.Institucion}";
                if (!string.IsNullOrEmpty(estudio.Ubicacion))
                    institucion += $" - {estudio.Ubicacion}";

                Paragraph inst = new Paragraph(institucion, fuenteTexto);
                inst.SpacingAfter = 5f;
                documento.Add(inst);

                // Tipo de estudio (Grado, curso, etc)
                if (!string.IsNullOrEmpty(estudio.TipoEstudio))
                {
                    Paragraph tipo = new Paragraph($"Tipo: {estudio.TipoEstudio}", fuentePequeña);
                    tipo.SpacingAfter = 15f;
                    documento.Add(tipo);
                }
                else
                {
                    documento.Add(new Paragraph(" ", fuentePequeña));
                }
            }
        }

        // Itera sobre la lista de experiencia laboral y la agrega al documento.
        private void GenerarExperienciaLaboral(Document documento, List<ExperienciaLaboral> experiencia)
        {
            if (experiencia == null || experiencia.Count == 0) return;

            Paragraph titulo = new Paragraph("EXPERIENCIA LABORAL", fuenteSubtitulo);
            titulo.SpacingAfter = 15f;
            documento.Add(titulo);

            foreach (var exp in experiencia)
            {
                // Formato: Cargo (FechaInicio - FechaFin)
                string fechas = $"{exp.FechaInicio:MM/yyyy} - {exp.FechaFin:MM/yyyy}";
                Paragraph cargo = new Paragraph($"{exp.Cargo} ({fechas})", fuenteNegrita);
                cargo.SpacingAfter = 5f;
                documento.Add(cargo);

                // Entidad o Empresa
                Paragraph empresa = new Paragraph(exp.Entidad, fuenteTexto);
                empresa.SpacingAfter = 15f;
                documento.Add(empresa);
            }
        }

        // Genera la lista de habilidades con formato de viñetas simples.
        private void GenerarHabilidades(Document documento, List<Habilidad> habilidades)
        {
            if (habilidades == null || habilidades.Count == 0) return;

            Paragraph titulo = new Paragraph("HABILIDADES", fuenteSubtitulo);
            titulo.SpacingAfter = 15f;
            documento.Add(titulo);

            foreach (var habilidad in habilidades)
            {
                string textoHabilidad = $"- {habilidad.Nombre}";

                if (!string.IsNullOrEmpty(habilidad.Competencia))
                    textoHabilidad += $" - {habilidad.Competencia}";

                if (!string.IsNullOrEmpty(habilidad.Dominio))
                    textoHabilidad += $" ({habilidad.Dominio})";

                Paragraph hab = new Paragraph(textoHabilidad, fuenteTexto);
                hab.SpacingAfter = 5f;
                documento.Add(hab);
            }

            documento.Add(new Paragraph(" ", fuenteTexto));
        }

        // Genera la seccion de referencias, agrupandolas por tipo (Personal, Laboral, etc).
        private void GenerarReferencias(Document documento, List<Referencia> referencias)
        {
            if (referencias == null || referencias.Count == 0) return;

            Paragraph titulo = new Paragraph("REFERENCIAS", fuenteSubtitulo);
            titulo.SpacingAfter = 15f;
            documento.Add(titulo);

            // Agrupamiento usando un Diccionario para separar por tipo.
            var referenciasPorTipo = new Dictionary<string, List<Referencia>>();

            foreach (var referencia in referencias)
            {
                string tipo = string.IsNullOrEmpty(referencia.Tipo) ? "Otras" : referencia.Tipo;
                if (!referenciasPorTipo.ContainsKey(tipo))
                    referenciasPorTipo[tipo] = new List<Referencia>();
                referenciasPorTipo[tipo].Add(referencia);
            }

            // Iteracion sobre los grupos y sus referencias internas.
            foreach (var grupo in referenciasPorTipo)
            {
                Paragraph tipoRef = new Paragraph($"{grupo.Key}:", fuenteNegrita);
                tipoRef.SpacingAfter = 8f;
                documento.Add(tipoRef);

                foreach (var referencia in grupo.Value)
                {
                    string textoReferencia = $"- {referencia.Nombre}";
                    if (!string.IsNullOrEmpty(referencia.Telefono))
                        textoReferencia += $" - {referencia.Telefono}";

                    Paragraph refPara = new Paragraph(textoReferencia, fuenteTexto);
                    refPara.SpacingAfter = 5f;
                    documento.Add(refPara);
                }

                documento.Add(new Paragraph(" ", fuentePequeña));
            }
        }

        // Genera el pie de pagina con una linea separadora y la fecha de generacion.
        private void GenerarPiePagina(Document documento)
        {
            documento.Add(new Paragraph(" ", fuenteTexto));
            iTextSharp.text.pdf.draw.LineSeparator linea = new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, ColorSecundario, Element.ALIGN_CENTER, -2);
            documento.Add(new Chunk(linea));

            Paragraph pie = new Paragraph($"CV generado el {DateTime.Now:dd/MM/yyyy} - Sistema GestorRRHH", fuentePequeña);
            pie.Alignment = Element.ALIGN_CENTER;
            pie.SpacingBefore = 10f;
            documento.Add(pie);
        }

        // Abre un cuadro de dialogo para que el usuario seleccione donde guardar el archivo PDF.
        private string SeleccionarRutaDestino(string nombreEmpleado)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Archivos PDF (*.pdf)|*.pdf",
                    Title = "Guardar CV en PDF",
                    // Se sugiere un nombre de archivo por defecto basado en el nombre y fecha.
                    FileName = $"CV_{nombreEmpleado.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                return saveDialog.ShowDialog() == DialogResult.OK ? saveDialog.FileName : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al seleccionar ruta: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // Metodo estatico de conveniencia para llamar a la generacion sin instanciar manualmente la clase.
        public static bool GenerarCVPDF(int idUsuario)
        {
            var generador = new GeneradorCVPDF();
            return generador.GenerarCV(idUsuario);
        }
    }

    // Clase interna que sirve como contenedor de datos (DTO).
    // Agrupa toda la informacion necesaria para pasarla al generador de PDF.
    internal class DatosCV
    {
        public string Nombre { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Correo { get; set; } = "";
        public string Departamento { get; set; } = "";
        public string ObjetivoProfesional { get; set; } = "";
        public byte[] Foto { get; set; }
        public List<FormacionAcademica> Estudios { get; set; } = new List<FormacionAcademica>();
        public List<ExperienciaLaboral> Experiencia { get; set; } = new List<ExperienciaLaboral>();
        public List<Habilidad> Habilidades { get; set; } = new List<Habilidad>();
        public List<Referencia> Referencias { get; set; } = new List<Referencia>();
    }
}