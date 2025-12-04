# Tutorial: Programación Orientada a Eventos - Sistema RRHH

## 📚 Introducción

Este tutorial explica cómo funciona la **programación orientada a eventos** usando un sistema de gestión de Recursos Humanos como ejemplo práctico.

## ¿Qué es la Programación Orientada a Eventos?

La programación orientada a eventos es un paradigma donde el flujo del programa está determinado por **eventos** (acciones que ocurren) en lugar de seguir una secuencia lineal de instrucciones.

### Componentes Principales

1. **Eventos**: Representan algo que ha sucedido en el sistema
2. **Event Bus**: Canal central que distribuye eventos
3. **Publicadores (Publishers)**: Componentes que generan eventos
4. **Suscriptores (Subscribers)**: Componentes que reaccionan a eventos

## 🏗️ Arquitectura del Sistema

```
┌─────────────────┐
│   HR Manager    │  ← Publica eventos cuando ocurren acciones
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Event Bus    │  ← Distribuye eventos a suscriptores
└────────┬────────┘
         │
         ├──────────────┬──────────────┬──────────────┐
         ▼              ▼              ▼              ▼
    ┌─────────┐   ┌─────────┐   ┌─────────┐   ┌─────────┐
    │Notifica │   │Auditoría│   │Capacidad│   │ Nómina  │
    └─────────┘   └─────────┘   └─────────┘   └─────────┘
```

## 🎯 Ejemplo Paso a Paso

### Paso 1: Crear el Event Bus

```python
from event_system import EventBus

event_bus = EventBus()
```

El Event Bus es el corazón del sistema. Maneja:
- Registro de suscriptores
- Distribución de eventos
- Historial de eventos

### Paso 2: Crear Manejadores

```python
from hr_handlers import NotificationHandler, AuditLogHandler

notificador = NotificationHandler()
auditor = AuditLogHandler()
```

Los manejadores son componentes que **reaccionan** a eventos.

### Paso 3: Suscribir Manejadores

```python
event_bus.subscribe("employee.hired", notificador.handle)
event_bus.subscribe("employee.hired", auditor.handle)
```

Aquí establecemos:
- **QUÉ** evento nos interesa (`employee.hired`)
- **QUIÉN** va a manejarlo (`notificador.handle`)

### Paso 4: Publicar Eventos

```python
from hr_manager import HRManager

hr = HRManager(event_bus)
dept = hr.create_department("dept_001", "Tecnología", 500000.0)
position = hr.create_position("pos_001", "Developer", 2, 60000.0, "dept_001")

# Esta acción publica automáticamente un evento "employee.hired"
employee = hr.hire_employee(
    "emp_001", "Juan", "Pérez", "juan@empresa.com",
    position, dept, 60000.0
)
```

## 🔄 Flujo de un Evento

1. **Acción**: `hr.hire_employee(...)` se ejecuta
2. **Creación**: Se crea un `EmployeeHiredEvent`
3. **Publicación**: El evento se publica en el `EventBus`
4. **Distribución**: El EventBus envía el evento a todos los suscriptores
5. **Procesamiento**: Cada manejador procesa el evento
   - NotificationHandler → Envía notificación
   - AuditLogHandler → Registra en log
   - DepartmentCapacityHandler → Actualiza contador
   - PayrollHandler → Actualiza nómina total

## 🎨 Ventajas del Enfoque

### 1. Desacoplamiento

Los componentes no se conocen entre sí:

```python
# ❌ Enfoque tradicional (acoplado)
def hire_employee(...):
    employee = Employee(...)
    notificador.notify(employee)    # Dependencia directa
    auditor.log(employee)           # Dependencia directa
    capacity.update(employee)       # Dependencia directa
    payroll.add(employee)           # Dependencia directa

# ✅ Enfoque orientado a eventos (desacoplado)
def hire_employee(...):
    employee = Employee(...)
    event_bus.publish(EmployeeHiredEvent(employee))
    # ¡Listo! El EventBus se encarga del resto
```

### 2. Extensibilidad

Agregar nueva funcionalidad es fácil:

```python
# Crear un nuevo manejador
class EmailHandler(EventHandler):
    def handle(self, event: Event):
        # Enviar email real
        send_email(event.data['email'], f"Bienvenido {event.data['employee_name']}")

# Suscribir (sin modificar código existente)
email_handler = EmailHandler()
event_bus.subscribe("employee.hired", email_handler.handle)
```

### 3. Historial y Auditoría

Todos los eventos quedan registrados:

```python
# Obtener historial completo
history = event_bus.get_history()
print(f"Total de eventos: {len(history)}")

# Ver todos los logs de auditoría
audit_handler.print_logs()
```

## 📝 Crear un Evento Personalizado

```python
from event_system import Event
from hr_models import Employee

class EmployeeOnVacationEvent(Event):
    def __init__(self, employee: Employee, days: int):
        super().__init__(
            event_type="employee.on_vacation",
            data={
                "employee_id": employee.id,
                "employee_name": employee.full_name,
                "vacation_days": days
            }
        )
        self.employee = employee
        self.days = days
```

## 🔧 Crear un Manejador Personalizado

```python
from event_system import EventHandler, Event

class VacationBalanceHandler(EventHandler):
    def __init__(self):
        self.balances = {}
    
    def handle(self, event: Event):
        if event.event_type == "employee.on_vacation":
            emp_id = event.data['employee_id']
            days = event.data['vacation_days']
            
            if emp_id not in self.balances:
                self.balances[emp_id] = 30  # 30 días al año
            
            self.balances[emp_id] -= days
            print(f"Días restantes: {self.balances[emp_id]}")

# Usar el manejador
vacation_handler = VacationBalanceHandler()
event_bus.subscribe("employee.on_vacation", vacation_handler.handle)

# Publicar evento
event = EmployeeOnVacationEvent(employee, 5)
event_bus.publish(event)
```

## 🎓 Conceptos Clave

### Patrón Observer

El sistema implementa el patrón Observer:
- **Subject**: EventBus
- **Observers**: Manejadores (handlers)
- **Notify**: `publish()` method

### Patrón Publisher-Subscriber

Separación clara entre:
- **Publishers**: HRManager genera eventos
- **Subscribers**: Handlers reaccionan a eventos
- **Broker**: EventBus conecta ambos

### Single Responsibility Principle

Cada componente tiene una única responsabilidad:
- `EventBus`: Gestionar suscripciones y distribuir eventos
- `NotificationHandler`: Solo notificar
- `AuditLogHandler`: Solo registrar
- `HRManager`: Solo gestionar operaciones de RRHH

## 💡 Casos de Uso Reales

Este patrón es común en:
- **Aplicaciones web**: React, Angular, Vue.js
- **Sistemas de mensajería**: RabbitMQ, Kafka
- **GUI**: Clicks, eventos de teclado
- **Microservicios**: Comunicación asíncrona
- **IoT**: Sensores generan eventos

## 🚀 Ejercicios Propuestos

1. **Agregar evento de evaluación de desempeño**
   - Crear `PerformanceReviewEvent`
   - Crear manejador que ajuste salario basado en calificación

2. **Agregar evento de capacitación**
   - Crear `TrainingCompletedEvent`
   - Registrar certificaciones del empleado

3. **Agregar límites de presupuesto**
   - Crear manejador que rechace contrataciones si exceden presupuesto

4. **Agregar notificaciones por email real**
   - Integrar con servicio de email (SendGrid, AWS SES)

5. **Agregar persistencia**
   - Guardar eventos en base de datos
   - Poder "replay" eventos

## 📖 Recursos Adicionales

- [Event-Driven Architecture - Martin Fowler](https://martinfowler.com/articles/201701-event-driven.html)
- [Observer Pattern - Refactoring Guru](https://refactoring.guru/design-patterns/observer)
- [Pub/Sub Pattern - Microsoft](https://docs.microsoft.com/en-us/azure/architecture/patterns/publisher-subscriber)

## 🤝 Conclusión

La programación orientada a eventos permite crear sistemas:
- ✅ Escalables
- ✅ Mantenibles
- ✅ Testables
- ✅ Extensibles
- ✅ Desacoplados

¡Practica creando tus propios eventos y manejadores!
