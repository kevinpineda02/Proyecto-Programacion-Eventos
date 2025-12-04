using System;
using System.Collections.Generic;

namespace GestorRRHH.Base_de_datos
{
    // --- MOLDES ---
    // Esta sección define las clases que sirven como estructura (entidades) 
    // para manejar la información académica, laboral, habilidades y referencias.

    // Clase que representa un registro individual de educación o formación académica.
    public class FormacionAcademica
    {
        // Identificador único para el registro de formación.
        public int IdFormacion { get; set; }

        // Nombre de la universidad, escuela o centro de estudios.
        public string Institucion { get; set; }

        // Nombre de la carrera, curso o grado obtenido.
        public string Titulo { get; set; }

        // Ciudad o país donde se encuentra la institución.
        public string Ubicacion { get; set; }

        // Fecha en la que iniciaron los estudios.
        public DateTime FechaInicio { get; set; }

        // Fecha en la que finalizaron los estudios.
        public DateTime FechaFin { get; set; }

        // Categoría del estudio (ej. Grado, Maestría, Curso técnico).
        public string TipoEstudio { get; set; }
    }

    // Clase que representa un registro de experiencia laboral previa.
    public class ExperienciaLaboral
    {
        // Identificador único para el registro de experiencia.
        public int IdExperiencia { get; set; }

        // Puesto o cargo desempeñado en la empresa.
        public string Cargo { get; set; }

        // Nombre de la empresa o entidad contratante.
        public string Entidad { get; set; }

        // Fecha de inicio de la relación laboral.
        public DateTime FechaInicio { get; set; }

        // Fecha de finalización de la relación laboral.
        public DateTime FechaFin { get; set; }
    }

    // Clase que representa una habilidad técnica o blanda del usuario.
    public class Habilidad
    {
        // Identificador único para el registro de habilidad.
        public int IdHabilidad { get; set; }

        // Nombre de la habilidad (ej. C#, Liderazgo, Excel).
        public string Nombre { get; set; }

        // Tipo de competencia (ej. Técnica, Blanda/Soft Skill).
        public string Competencia { get; set; }

        // Nivel de dominio sobre la habilidad (ej. Básico, Intermedio, Avanzado).
        public string Dominio { get; set; }
    }

    // Clase que representa una referencia personal o laboral.
    public class Referencia
    {
        // Identificador único para el registro de referencia.
        public int IdReferencia { get; set; }

        // Nombre completo de la persona que da la referencia.
        public string Nombre { get; set; }

        // Número de contacto de la referencia.
        public string Telefono { get; set; }

        // Tipo de relación (ej. Jefe anterior, Colega, Amigo).
        public string Tipo { get; set; }
    }

    /// <summary>
    /// Clase estática usada como "memoria compartida" entre formularios.
    /// Se usa para editar CV, tanto completo como parcial.
    /// Esta clase permite pasar datos de una ventana a otra sin guardarlos inmediatamente en la base de datos.
    /// </summary>
    public static class DatosCompartidos
    {
        // Variable para controlar si un Administrador está viendo el perfil de otro usuario.
        // Si es 0, se asume que es el usuario normal viendo su propio perfil.
        public static int IdUsuarioSeleccionado { get; set; } = 0;

        // DATOS PERSONALES
        // Almacena temporalmente el nombre completo del usuario.
        public static string NombreCompleto { get; set; }

        // Almacena temporalmente el número de teléfono.
        public static string Telefono { get; set; }

        // Almacena temporalmente el correo electrónico.
        public static string Correo { get; set; }

        // Almacena el departamento o área al que pertenece el empleado.
        public static string Departamento { get; set; }

        // Almacena un breve resumen o descripción del perfil profesional.
        public static string ObjetivoProfesional { get; set; }

        // Almacena la fotografía del usuario en formato de arreglo de bytes (para bases de datos).
        public static byte[] Foto { get; set; }

        // --- LISTAS DE CV (MEMORIA) ---
        // Listas que mantienen los registros en memoria mientras se edita el perfil,
        // antes de confirmar los cambios en la base de datos principal.

        // Lista temporal de formación académica.
        public static List<FormacionAcademica> ListaEstudios { get; set; } = new List<FormacionAcademica>();

        // Lista temporal de experiencia laboral.
        public static List<ExperienciaLaboral> ListaExperiencia { get; set; } = new List<ExperienciaLaboral>();

        // Lista temporal de habilidades.
        public static List<Habilidad> ListaHabilidades { get; set; } = new List<Habilidad>();

        // Lista temporal de referencias.
        public static List<Referencia> ListaReferencias { get; set; } = new List<Referencia>();

        // --- OBJETOS PARA EDICIÓN PUNTUAL ---
        // Estas propiedades sirven para cargar un solo elemento cuando el usuario selecciona "Editar" en una fila específica.
        // Si son nulos, significa que se está creando un registro nuevo.

        // Objeto temporal para editar una formación específica.
        public static FormacionAcademica EstudioAEditar { get; set; } = null;

        // Objeto temporal para editar una experiencia laboral específica.
        public static ExperienciaLaboral ExperienciaAEditar { get; set; } = null;

        // Objeto temporal para editar una habilidad específica.
        public static Habilidad HabilidadAEditar { get; set; } = null;

        // Objeto temporal para editar una referencia específica.
        public static Referencia ReferenciaAEditar { get; set; } = null;

        // Bandera que indica el modo de edición:
        // True = El usuario está recorriendo todo el asistente de edición de CV.
        // False = El usuario solo entró a editar una sección específica (ej. solo cambiar el teléfono).
        public static bool EsEdicionCompleta { get; set; } = false;

        /// <summary>
        /// Limpia todos los datos temporales.
        /// Este método es vital para asegurar que no queden datos de una sesión o edición anterior
        /// cuando se abre el formulario nuevamente.
        /// </summary>
        public static void Limpiar()
        {
            // Se restablecen a nulo todas las variables de datos personales.
            NombreCompleto = null;
            Telefono = null;
            Correo = null;
            Departamento = null;
            ObjetivoProfesional = null;
            Foto = null;

            // Se reinicializan las listas para que estén vacías.
            ListaEstudios = new List<FormacionAcademica>();
            ListaExperiencia = new List<ExperienciaLaboral>();
            ListaHabilidades = new List<Habilidad>();
            ListaReferencias = new List<Referencia>();

            // Se limpian los objetos de edición puntual.
            EstudioAEditar = null;
            ExperienciaAEditar = null;
            HabilidadAEditar = null;
            ReferenciaAEditar = null;

            // Se establece el modo de edición a falso por defecto.
            EsEdicionCompleta = false;

            // Resetear selección de admin para evitar ver el perfil incorrecto en el futuro.
            IdUsuarioSeleccionado = 0;
        }


        /// <summary>
        /// Si Admin seleccionó un usuario, devuelve ese ID.
        /// Si no, devuelve 0 para que el formulario use SesionActual.IdUsuario.
        /// </summary>
        // Método auxiliar para determinar qué ID de usuario se debe consultar en la base de datos.
        public static int ObtenerIdUsuarioSeleccionado()
        {
            return IdUsuarioSeleccionado;
        }
    }
}