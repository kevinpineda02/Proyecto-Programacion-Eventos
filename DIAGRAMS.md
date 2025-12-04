# Diagramas del Sistema

## Arquitectura General

```
┌─────────────────────────────────────────────────────────────────┐
│                        USUARIO / APLICACIÓN                      │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                         HR MANAGER                               │
│  • hire_employee()      • promote_employee()                    │
│  • terminate_employee() • transfer_employee()                   │
│  • adjust_salary()      • create_department()                   │
└────────────────────────┬────────────────────────────────────────┘
                         │ publica eventos
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                         EVENT BUS                                │
│  • subscribe()   • publish()   • get_history()                  │
└────────────┬────────────────────────────────────────────────────┘
             │ distribuye a suscriptores
             │
    ┌────────┼─────────┬──────────┬─────────────┐
    ▼        ▼         ▼          ▼             ▼
┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
│Notifica│ │Auditoría│ │Capacidad│ │ Nómina │ │Custom  │
│Handler │ │Handler  │ │Handler  │ │Handler │ │Handler │
└────────┘ └────────┘ └────────┘ └────────┘ └────────┘
```

## Flujo de un Evento

```
PASO 1: Acción del Usuario
────────────────────────────
hr.hire_employee("emp_001", "Juan", "Pérez", ...)
         │
         ▼

PASO 2: HR Manager crea Employee y Event
─────────────────────────────────────────
employee = Employee(...)
event = EmployeeHiredEvent(employee)
         │
         ▼

PASO 3: Publicación en Event Bus
─────────────────────────────────
event_bus.publish(event)
         │
         ├─────────────────┬─────────────────┬─────────────────┐
         ▼                 ▼                 ▼                 ▼

PASO 4: Handlers Procesan en Paralelo
──────────────────────────────────────
NotificationHandler:    AuditLogHandler:    CapacityHandler:   PayrollHandler:
"¡Bienvenido Juan!"     LOG: employee.hired  IT: 1 → 2 emps     +$60,000
```

## Ciclo de Vida de un Empleado

```
┌─────────────────┐
│   CONTRATACIÓN  │
│  hire_employee()│
└────────┬────────┘
         │ EmployeeHiredEvent
         ▼
┌─────────────────┐
│     ACTIVO      │
│  Status: ACTIVE │ ────────┐
└────────┬────────┘         │
         │                  │ Múltiples eventos posibles:
         │                  │ • EmployeePromotedEvent
         │                  │ • DepartmentChangedEvent
         │                  │ • SalaryAdjustedEvent
         │ ◄────────────────┘
         ▼
┌─────────────────┐
│   TERMINACIÓN   │
│terminate_employee()
└────────┬────────┘
         │ EmployeeTerminatedEvent
         ▼
┌─────────────────┐
│   TERMINADO     │
│Status: TERMINATED
└─────────────────┘
```

## Estructura de Eventos

```
┌─────────────────────────────────────────────────┐
│                    EVENT                        │
├─────────────────────────────────────────────────┤
│ event_type: string                              │
│   "employee.hired"                              │
│   "employee.terminated"                         │
│   "employee.promoted"                           │
│   "employee.department_changed"                 │
│   "employee.salary_adjusted"                    │
│   "department.created"                          │
├─────────────────────────────────────────────────┤
│ timestamp: datetime                             │
│   2025-12-04T10:30:00                          │
├─────────────────────────────────────────────────┤
│ data: dict                                      │
│   {                                             │
│     "employee_id": "emp_001",                   │
│     "employee_name": "Juan Pérez",              │
│     "position": "Developer",                    │
│     "department": "IT",                         │
│     "salary": 60000.0,                          │
│     ...                                         │
│   }                                             │
└─────────────────────────────────────────────────┘
```

## Modelo de Datos

```
┌─────────────────────┐
│     Department      │
├─────────────────────┤
│ id: string          │
│ name: string        │
│ budget: float       │
│ max_employees: int  │
└─────────┬───────────┘
          │ 1
          │
          │ N
┌─────────┴───────────┐         ┌──────────────────┐
│      Position       │         │     Employee     │
├─────────────────────┤    N    ├──────────────────┤
│ id: string          │◄────────┤ id: string       │
│ title: string       │         │ first_name: str  │
│ level: int          │         │ last_name: str   │
│ base_salary: float  │         │ email: string    │
│ department_id: str  │         │ salary: float    │
└─────────────────────┘         │ status: enum     │
                                │ hire_date: date  │
                                └──────────────────┘
```

## Patrón Publisher-Subscriber

```
┌──────────────────────────────────────────────────────────────┐
│                      PUBLISHERS                               │
│  (Generan eventos cuando ocurren cambios)                    │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  HRManager:                                                   │
│    • hire_employee()      → EmployeeHiredEvent               │
│    • terminate_employee() → EmployeeTerminatedEvent          │
│    • promote_employee()   → EmployeePromotedEvent            │
│    • adjust_salary()      → SalaryAdjustedEvent              │
│                                                               │
└───────────────────────────┬──────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│                      EVENT BUS                                │
│  (Mediador que conecta publishers con subscribers)           │
│                                                               │
│  Responsabilidades:                                           │
│  • Mantener lista de suscriptores por tipo de evento         │
│  • Distribuir eventos a suscriptores interesados             │
│  • Mantener historial de eventos                             │
│                                                               │
└───────────────────────────┬──────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│                      SUBSCRIBERS                              │
│  (Reaccionan a eventos cuando son publicados)                │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  NotificationHandler:                                         │
│    → employee.hired: "¡Bienvenido [nombre]!"                 │
│    → employee.promoted: "¡Felicidades por la promoción!"     │
│                                                               │
│  AuditLogHandler:                                             │
│    → ALL events: Registra en log con timestamp               │
│                                                               │
│  DepartmentCapacityHandler:                                   │
│    → employee.hired: incrementa contador                     │
│    → employee.terminated: decrementa contador                │
│                                                               │
│  PayrollHandler:                                              │
│    → employee.hired: suma salario a nómina                   │
│    → employee.terminated: resta salario de nómina            │
│    → employee.salary_adjusted: ajusta nómina total           │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

## Ventajas del Desacoplamiento

```
SIN Event-Driven (Acoplado)
───────────────────────────────────────────────────────────
def hire_employee(employee):
    db.save(employee)                  # Dependencia 1
    notification_service.send(employee) # Dependencia 2
    audit_log.write(employee)          # Dependencia 3
    capacity_tracker.increment(dept)   # Dependencia 4
    payroll.add_to_total(salary)       # Dependencia 5
    
Problemas:
❌ HR Manager conoce TODOS los servicios
❌ Difícil agregar nuevo servicio
❌ Cambios en un servicio afectan HR Manager
❌ Difícil de testear
❌ Alto acoplamiento


CON Event-Driven (Desacoplado)
───────────────────────────────────────────────────────────
def hire_employee(employee):
    event_bus.publish(EmployeeHiredEvent(employee))
    
Ventajas:
✅ HR Manager solo conoce el EventBus
✅ Fácil agregar nuevo handler (2 líneas)
✅ Cambios en handlers NO afectan HR Manager
✅ Fácil de testear (mock del EventBus)
✅ Bajo acoplamiento
```

## Extensibilidad

```
Agregar Nueva Funcionalidad: Email Notifications
────────────────────────────────────────────────

1. Crear el Handler
   ┌──────────────────────────────────────┐
   │  class EmailHandler(EventHandler):   │
   │    def handle(self, event):          │
   │      send_email(...)                 │
   └──────────────────────────────────────┘

2. Suscribir al EventBus
   ┌──────────────────────────────────────┐
   │  email_handler = EmailHandler()      │
   │  event_bus.subscribe(                │
   │    "employee.hired",                 │
   │    email_handler.handle              │
   │  )                                   │
   └──────────────────────────────────────┘

3. ¡Listo! Ya funciona
   ✅ No se modificó HR Manager
   ✅ No se modificaron otros handlers
   ✅ Sistema sigue funcionando igual
   ✅ Nueva funcionalidad operativa
```

## Comparación con Otros Patrones

```
┌────────────────────┬──────────────────┬─────────────────────┐
│  Patrón            │  Acoplamiento    │  Escalabilidad      │
├────────────────────┼──────────────────┼─────────────────────┤
│  Direct Calls      │  Alto ❌         │  Baja ❌            │
│  (Tradicional)     │                  │                     │
├────────────────────┼──────────────────┼─────────────────────┤
│  Observer          │  Medio ⚠️        │  Media ⚠️           │
│  (Directo)         │                  │                     │
├────────────────────┼──────────────────┼─────────────────────┤
│  Event Bus         │  Bajo ✅         │  Alta ✅            │
│  (Este sistema)    │                  │                     │
└────────────────────┴──────────────────┴─────────────────────┘
```
