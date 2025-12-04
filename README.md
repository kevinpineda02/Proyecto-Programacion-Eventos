# Sistema de Recursos Humanos Orientado a Eventos 🏢

Sistema de gestión de Recursos Humanos (RRHH) implementado con **programación orientada a eventos** en Python.

## 📋 Descripción

Este proyecto demuestra los principios de la **programación orientada a eventos** aplicados a un sistema de gestión de recursos humanos. El sistema permite gestionar empleados, departamentos y posiciones, donde cada acción genera eventos que son procesados por múltiples manejadores de forma desacoplada.

## 🎯 Características Principales

### Arquitectura Orientada a Eventos
- **EventBus**: Sistema central de publicación/suscripción de eventos
- **Event**: Clase base para todos los eventos del sistema
- **EventHandler**: Interfaz para implementar manejadores de eventos

### Gestión de RRHH
- ✅ Contratación de empleados
- ✅ Promociones y cambios de posición
- ✅ Transferencias entre departamentos
- ✅ Ajustes salariales
- ✅ Terminación de contratos
- ✅ Gestión de departamentos

### Manejadores de Eventos
1. **NotificationHandler**: Envía notificaciones cuando ocurren eventos
2. **AuditLogHandler**: Registra todos los eventos en un log de auditoría
3. **DepartmentCapacityHandler**: Controla la capacidad de empleados por departamento
4. **PayrollHandler**: Calcula y rastrea la nómina total de la empresa

## 🏗️ Estructura del Proyecto

```
Proyecto-Programacion-Eventos/
├── event_system.py      # Sistema base de eventos (EventBus, Event, EventHandler)
├── hr_models.py         # Modelos de dominio (Employee, Department, Position)
├── hr_events.py         # Eventos específicos de RRHH
├── hr_handlers.py       # Manejadores de eventos
├── hr_manager.py        # Gestor principal del sistema de RRHH
├── demo.py              # Demostración completa del sistema
└── README.md            # Este archivo
```

## 🚀 Uso

### Requisitos
- Python 3.7 o superior

### Ejecutar la Demostración

```bash
python demo.py
```

### Ejemplo de Uso Básico

```python
from event_system import EventBus
from hr_manager import HRManager
from hr_handlers import NotificationHandler, AuditLogHandler

# 1. Crear el bus de eventos
event_bus = EventBus()

# 2. Crear y suscribir manejadores
notification_handler = NotificationHandler()
audit_handler = AuditLogHandler()

event_bus.subscribe("employee.hired", notification_handler.handle)
event_bus.subscribe("employee.hired", audit_handler.handle)

# 3. Crear el gestor de RRHH
hr_manager = HRManager(event_bus)

# 4. Crear un departamento
dept = hr_manager.create_department("dept_001", "Tecnología", 500000.0)

# 5. Crear una posición
position = hr_manager.create_position(
    "pos_001", "Desarrollador", 2, 60000.0, "dept_001"
)

# 6. Contratar un empleado (genera evento EmployeeHiredEvent)
employee = hr_manager.hire_employee(
    "emp_001", "Juan", "Pérez", "juan@empresa.com",
    position, dept, 60000.0
)

# 7. Promover empleado (genera eventos EmployeePromotedEvent y SalaryAdjustedEvent)
senior_position = hr_manager.create_position(
    "pos_002", "Desarrollador Senior", 3, 80000.0, "dept_001"
)
hr_manager.promote_employee("emp_001", senior_position)
```

## 📚 Eventos Disponibles

| Evento | Descripción | Datos |
|--------|-------------|-------|
| `employee.hired` | Un empleado ha sido contratado | employee_id, name, position, department, salary |
| `employee.terminated` | Un empleado ha dejado la empresa | employee_id, name, reason |
| `employee.promoted` | Un empleado ha sido promovido | employee_id, old_position, new_position |
| `employee.department_changed` | Un empleado cambió de departamento | employee_id, old_department, new_department |
| `employee.salary_adjusted` | El salario de un empleado fue ajustado | employee_id, old_salary, new_salary, percentage |
| `department.created` | Se ha creado un nuevo departamento | department_id, name, budget |

## 🎓 Conceptos Demostrados

### Programación Orientada a Eventos
- **Desacoplamiento**: Los componentes no se conocen entre sí, solo reaccionan a eventos
- **Extensibilidad**: Se pueden agregar nuevos manejadores sin modificar código existente
- **Patrón Publisher-Subscriber**: Separación entre quién emite eventos y quién los consume
- **Single Responsibility**: Cada manejador tiene una responsabilidad específica

### Ventajas del Enfoque
- ✅ Fácil de extender con nuevos manejadores
- ✅ Componentes independientes y reutilizables
- ✅ Facilita el testing unitario
- ✅ Historial completo de eventos para auditoría
- ✅ Flujo de datos claro y predecible

## 🔧 Personalización

### Agregar un Nuevo Manejador

```python
from event_system import EventHandler, Event

class CustomHandler(EventHandler):
    def handle(self, event: Event) -> None:
        # Tu lógica personalizada
        print(f"Procesando evento: {event.event_type}")

# Suscribir al bus de eventos
custom_handler = CustomHandler()
event_bus.subscribe("employee.hired", custom_handler.handle)
```

### Crear un Nuevo Tipo de Evento

```python
from event_system import Event
from hr_models import Employee

class EmployeeTrainingEvent(Event):
    def __init__(self, employee: Employee, training_name: str):
        super().__init__(
            event_type="employee.training_completed",
            data={
                "employee_id": employee.id,
                "employee_name": employee.full_name,
                "training": training_name
            }
        )
        self.employee = employee
        self.training_name = training_name
```

## 📊 Salida de la Demostración

La demostración muestra:
1. Inicialización del sistema
2. Creación de departamentos y posiciones
3. Contratación de empleados
4. Promociones y ajustes salariales
5. Transferencias entre departamentos
6. Terminaciones de contrato
7. Estadísticas y resúmenes
8. Log completo de auditoría

## 👥 Autor

Sistema educativo de demostración de programación orientada a eventos.

## 📄 Licencia

Proyecto educativo - libre para uso y modificación.
