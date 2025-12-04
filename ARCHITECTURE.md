# Arquitectura del Sistema - Event-Driven HR

## Visión General

Este documento describe la arquitectura del sistema de gestión de Recursos Humanos orientado a eventos.

## Principios de Diseño

### 1. Event-Driven Architecture (EDA)

El sistema utiliza **arquitectura orientada a eventos** donde:
- Los componentes se comunican a través de eventos
- No hay dependencias directas entre componentes
- Cada evento representa un hecho que ocurrió en el sistema

### 2. Publisher-Subscriber Pattern

```
Publisher (HR Manager) 
    ↓ publica evento
EventBus (mediador)
    ↓ notifica a
Subscribers (Handlers)
```

### 3. Single Responsibility Principle

Cada componente tiene una única responsabilidad:
- **EventBus**: Solo gestiona publicación/suscripción
- **HRManager**: Solo ejecuta operaciones de RRHH
- **NotificationHandler**: Solo envía notificaciones
- **AuditLogHandler**: Solo registra logs
- etc.

## Componentes del Sistema

### Core (`event_system.py`)

#### Event
- Clase base para todos los eventos
- Contiene: `event_type`, `data`, `timestamp`
- Inmutable después de la creación

#### EventBus
- **Responsabilidad**: Gestionar suscripciones y distribuir eventos
- **Métodos clave**:
  - `subscribe(event_type, handler)`: Registrar suscriptor
  - `publish(event)`: Distribuir evento a suscriptores
  - `get_history()`: Obtener historial completo

#### EventHandler
- Interfaz abstracta para handlers
- Define método `handle(event)` que debe implementarse

### Domain Models (`hr_models.py`)

#### Employee
- Representa un empleado
- Campos: id, nombre, email, position, department, salary, status
- Status: ACTIVE, INACTIVE, ON_LEAVE, TERMINATED

#### Department
- Representa un departamento
- Campos: id, name, budget, max_employees

#### Position
- Representa una posición/cargo
- Campos: id, title, level, base_salary

### Events (`hr_events.py`)

Eventos específicos del dominio HR:

1. **EmployeeHiredEvent**: Nuevo empleado contratado
2. **EmployeeTerminatedEvent**: Empleado termina contrato
3. **EmployeePromotedEvent**: Empleado promovido
4. **DepartmentChangedEvent**: Empleado cambia de departamento
5. **SalaryAdjustedEvent**: Ajuste salarial
6. **DepartmentCreatedEvent**: Nuevo departamento

#### Estructura de un Evento

```python
{
    "event_type": "employee.hired",
    "timestamp": "2025-12-04T10:30:00",
    "data": {
        "employee_id": "emp_001",
        "employee_name": "Juan Pérez",
        "position": "Developer",
        "department": "IT",
        "salary": 60000.0
    }
}
```

### Handlers (`hr_handlers.py`)

#### NotificationHandler
- **Propósito**: Enviar notificaciones a usuarios
- **Reacciona a**: Todos los eventos
- **Salida**: Mensajes formateados por consola

#### AuditLogHandler
- **Propósito**: Mantener registro de auditoría
- **Reacciona a**: Todos los eventos
- **Salida**: Log estructurado con timestamp

#### DepartmentCapacityHandler
- **Propósito**: Rastrear número de empleados por departamento
- **Reacciona a**: hired, terminated, department_changed
- **Estado**: Mapa de departamento → contador

#### PayrollHandler
- **Propósito**: Calcular nómina total
- **Reacciona a**: hired, terminated, salary_adjusted
- **Estado**: total_payroll (float)

### HR Manager (`hr_manager.py`)

**Coordinador central del sistema**

#### Responsabilidades
1. Gestionar operaciones CRUD de empleados/departamentos
2. Publicar eventos cuando ocurren cambios
3. Mantener estado del sistema (empleados, departamentos, posiciones)

#### Métodos Principales
- `hire_employee()`: Contrata empleado → publica EmployeeHiredEvent
- `terminate_employee()`: Termina contrato → publica EmployeeTerminatedEvent
- `promote_employee()`: Promueve empleado → publica EmployeePromotedEvent
- `transfer_employee()`: Transfiere departamento → publica DepartmentChangedEvent
- `adjust_salary()`: Ajusta salario → publica SalaryAdjustedEvent

## Flujo de Datos

### Ejemplo: Contratar Empleado

```
1. Usuario llama: hr_manager.hire_employee(...)
                   ↓
2. HR Manager crea objeto Employee
                   ↓
3. HR Manager crea EmployeeHiredEvent
                   ↓
4. HR Manager llama: event_bus.publish(event)
                   ↓
5. EventBus notifica a todos los suscriptores:
   ├→ NotificationHandler.handle(event)
   ├→ AuditLogHandler.handle(event)
   ├→ DepartmentCapacityHandler.handle(event)
   └→ PayrollHandler.handle(event)
                   ↓
6. Cada handler procesa el evento independientemente
```

## Ventajas de la Arquitectura

### 1. Desacoplamiento
- Los handlers no conocen al HR Manager
- El HR Manager no conoce a los handlers
- Fácil agregar/quitar componentes

### 2. Escalabilidad
- Agregar nuevo handler: solo 2 líneas de código
- No requiere modificar código existente
- Cada handler se ejecuta independientemente

### 3. Testabilidad
- Componentes se pueden testear aisladamente
- Fácil hacer mock de eventos
- Historial de eventos facilita debugging

### 4. Auditabilidad
- Historial completo de eventos
- Cada cambio está registrado
- Posibilidad de "replay" de eventos

### 5. Mantenibilidad
- Responsabilidades claras
- Código modular
- Fácil de entender y modificar

## Patrones de Diseño Utilizados

1. **Observer Pattern**: EventBus + Handlers
2. **Publisher-Subscriber**: HRManager → EventBus → Handlers
3. **Event Sourcing**: Historial completo de eventos
4. **Single Responsibility**: Cada clase una responsabilidad
5. **Open/Closed Principle**: Abierto a extensión, cerrado a modificación

## Extensibilidad

### Agregar Nuevo Evento

```python
class EmployeeTrainingEvent(Event):
    def __init__(self, employee, training):
        super().__init__(
            event_type="employee.training_completed",
            data={...}
        )
```

### Agregar Nuevo Handler

```python
class EmailHandler(EventHandler):
    def handle(self, event):
        # enviar email
        pass

# Registrar
event_bus.subscribe("employee.hired", email_handler.handle)
```

### Agregar Nueva Operación

```python
# En HRManager
def give_bonus(self, emp_id, amount):
    # ... lógica ...
    event = BonusAwardedEvent(employee, amount)
    self.event_bus.publish(event)
```

## Consideraciones de Seguridad

1. **Validación de Datos**: Eventos contienen solo datos serializables
2. **División por Cero**: Protegido en cálculos de porcentajes
3. **Estado Consistente**: Eventos mantienen integridad referencial
4. **Sin Secretos**: Eventos no contienen información sensible (passwords, etc.)

## Limitaciones Conocidas

1. **Orden de Procesamiento**: Los handlers se ejecutan en orden de suscripción
2. **Sin Transacciones**: Si un handler falla, otros igual se ejecutan
3. **Memoria**: Historial de eventos crece indefinidamente (en producción usar persistencia)
4. **Sincronía**: Todos los handlers se ejecutan sincrónicamente

## Mejoras Futuras

1. **Persistencia**: Guardar eventos en base de datos
2. **Async Handlers**: Procesar eventos asíncronamente
3. **Event Replay**: Reconstruir estado desde historial
4. **Prioridad de Handlers**: Orden de ejecución configurable
5. **Retry Logic**: Reintentar handlers que fallan
6. **Dead Letter Queue**: Manejar eventos que no se pueden procesar
7. **Event Validation**: Validar estructura de eventos
8. **Performance Monitoring**: Métricas de rendimiento por handler

## Referencias

- Martin Fowler - Event-Driven Architecture
- Design Patterns (Gang of Four)
- Domain-Driven Design (Eric Evans)
- Enterprise Integration Patterns (Gregor Hohpe)
