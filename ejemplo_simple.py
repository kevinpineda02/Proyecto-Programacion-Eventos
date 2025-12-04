"""
Ejemplo Simple del Sistema de RRHH Orientado a Eventos
Simple Example of Event-Driven HR System
"""
from event_system import EventBus
from hr_manager import HRManager
from hr_handlers import NotificationHandler, AuditLogHandler


def main():
    print("=" * 60)
    print("  EJEMPLO SIMPLE - Sistema de RRHH Orientado a Eventos")
    print("=" * 60)
    
    # Paso 1: Crear el bus de eventos
    print("\n1. Creando el bus de eventos...")
    event_bus = EventBus()
    
    # Paso 2: Crear manejadores
    print("2. Creando manejadores de eventos...")
    notificador = NotificationHandler("Sistema de Notificaciones")
    auditor = AuditLogHandler()
    
    # Paso 3: Suscribir manejadores a eventos
    print("3. Suscribiendo manejadores...\n")
    event_bus.subscribe("employee.hired", notificador.handle)
    event_bus.subscribe("employee.hired", auditor.handle)
    event_bus.subscribe("employee.promoted", notificador.handle)
    event_bus.subscribe("employee.promoted", auditor.handle)
    event_bus.subscribe("employee.salary_adjusted", notificador.handle)
    
    # Paso 4: Crear el gestor de RRHH
    print("\n4. Creando el gestor de RRHH...")
    hr = HRManager(event_bus)
    
    # Paso 5: Crear un departamento
    print("\n5. Creando departamento...\n")
    tecnologia = hr.create_department("dept_001", "Tecnología", 500000.0)
    
    # Paso 6: Crear posiciones
    print("\n6. Creando posiciones...")
    junior = hr.create_position("pos_001", "Developer Junior", 1, 45000.0, "dept_001")
    senior = hr.create_position("pos_002", "Developer Senior", 3, 75000.0, "dept_001")
    
    # Paso 7: Contratar empleados
    print("\n7. Contratando empleados...\n")
    ana = hr.hire_employee(
        "emp_001", "Ana", "Silva", "ana@empresa.com",
        junior, tecnologia, 45000.0
    )
    
    pedro = hr.hire_employee(
        "emp_002", "Pedro", "Gómez", "pedro@empresa.com",
        junior, tecnologia, 45000.0
    )
    
    # Paso 8: Promover empleado
    print("\n8. Promocionando a Ana...\n")
    hr.promote_employee("emp_001", senior)
    
    # Paso 9: Ajustar salario
    print("\n9. Ajustando salario de Pedro...\n")
    hr.adjust_salary("emp_002", 50000.0, "Buen desempeño")
    
    # Resumen
    print("\n" + "=" * 60)
    print("  RESUMEN")
    print("=" * 60)
    print(f"\nEmpleados activos: {len(hr.get_active_employees())}")
    print(f"Eventos procesados: {len(event_bus.get_history())}")
    
    print("\nEmpleados en Tecnología:")
    for emp in hr.get_employees_by_department("dept_001"):
        print(f"  • {emp.full_name} - {emp.position.title} - ${emp.salary:,.2f}")
    
    print("\n✅ Ejemplo completado exitosamente!\n")


if __name__ == "__main__":
    main()
