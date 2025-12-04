"""
Demostración del Sistema de Recursos Humanos Orientado a Eventos
Event-Driven HR System Demo
"""
from event_system import EventBus
from hr_manager import HRManager
from hr_handlers import (
    NotificationHandler,
    AuditLogHandler,
    DepartmentCapacityHandler,
    PayrollHandler
)


def print_header(title: str):
    """Imprime un encabezado decorado"""
    print("\n" + "="*80)
    print(f"  {title}")
    print("="*80 + "\n")


def main():
    print_header("🏢 SISTEMA DE RECURSOS HUMANOS ORIENTADO A EVENTOS")
    print("Este sistema demuestra programación orientada a eventos aplicada a RRHH\n")
    
    # 1. Crear el EventBus
    print_header("1️⃣  INICIALIZACIÓN DEL SISTEMA")
    event_bus = EventBus()
    
    # 2. Crear y registrar manejadores de eventos
    print("\n📌 Registrando manejadores de eventos...\n")
    notification_handler = NotificationHandler("Sistema de Notificaciones HR")
    audit_handler = AuditLogHandler()
    capacity_handler = DepartmentCapacityHandler()
    payroll_handler = PayrollHandler()
    
    # Suscribir manejadores a eventos específicos
    event_bus.subscribe("employee.hired", notification_handler.handle)
    event_bus.subscribe("employee.hired", audit_handler.handle)
    event_bus.subscribe("employee.hired", capacity_handler.handle)
    event_bus.subscribe("employee.hired", payroll_handler.handle)
    
    event_bus.subscribe("employee.terminated", notification_handler.handle)
    event_bus.subscribe("employee.terminated", audit_handler.handle)
    event_bus.subscribe("employee.terminated", capacity_handler.handle)
    
    event_bus.subscribe("employee.promoted", notification_handler.handle)
    event_bus.subscribe("employee.promoted", audit_handler.handle)
    
    event_bus.subscribe("employee.department_changed", notification_handler.handle)
    event_bus.subscribe("employee.department_changed", audit_handler.handle)
    event_bus.subscribe("employee.department_changed", capacity_handler.handle)
    
    event_bus.subscribe("employee.salary_adjusted", notification_handler.handle)
    event_bus.subscribe("employee.salary_adjusted", audit_handler.handle)
    event_bus.subscribe("employee.salary_adjusted", payroll_handler.handle)
    
    event_bus.subscribe("department.created", notification_handler.handle)
    event_bus.subscribe("department.created", audit_handler.handle)
    
    # 3. Crear el HR Manager
    hr_manager = HRManager(event_bus)
    
    # 4. Crear departamentos
    print_header("2️⃣  CREACIÓN DE DEPARTAMENTOS")
    dept_it = hr_manager.create_department("dept_001", "Tecnología", 500000.0, 30)
    dept_sales = hr_manager.create_department("dept_002", "Ventas", 300000.0, 25)
    dept_hr = hr_manager.create_department("dept_003", "Recursos Humanos", 200000.0, 15)
    
    # 5. Crear posiciones
    print_header("3️⃣  CREACIÓN DE POSICIONES")
    print("Creando posiciones para los departamentos...\n")
    
    # Posiciones IT
    pos_dev_junior = hr_manager.create_position("pos_001", "Desarrollador Junior", 1, 45000.0, "dept_001")
    pos_dev_senior = hr_manager.create_position("pos_002", "Desarrollador Senior", 3, 75000.0, "dept_001")
    pos_tech_lead = hr_manager.create_position("pos_003", "Tech Lead", 5, 95000.0, "dept_001")
    
    # Posiciones Ventas
    pos_sales_rep = hr_manager.create_position("pos_004", "Representante de Ventas", 2, 40000.0, "dept_002")
    pos_sales_manager = hr_manager.create_position("pos_005", "Gerente de Ventas", 4, 70000.0, "dept_002")
    
    # Posiciones HR
    pos_hr_specialist = hr_manager.create_position("pos_006", "Especialista HR", 2, 50000.0, "dept_003")
    pos_hr_manager = hr_manager.create_position("pos_007", "Gerente HR", 4, 80000.0, "dept_003")
    
    print("✅ Posiciones creadas exitosamente\n")
    
    # 6. Contratar empleados
    print_header("4️⃣  CONTRATACIÓN DE EMPLEADOS")
    
    emp1 = hr_manager.hire_employee(
        "emp_001", "Juan", "Pérez", "juan.perez@empresa.com",
        pos_dev_senior, dept_it, 75000.0
    )
    
    emp2 = hr_manager.hire_employee(
        "emp_002", "María", "González", "maria.gonzalez@empresa.com",
        pos_dev_junior, dept_it, 45000.0, manager_id="emp_001"
    )
    
    emp3 = hr_manager.hire_employee(
        "emp_003", "Carlos", "Rodríguez", "carlos.rodriguez@empresa.com",
        pos_sales_manager, dept_sales, 70000.0
    )
    
    emp4 = hr_manager.hire_employee(
        "emp_004", "Ana", "Martínez", "ana.martinez@empresa.com",
        pos_hr_manager, dept_hr, 80000.0
    )
    
    emp5 = hr_manager.hire_employee(
        "emp_005", "Luis", "López", "luis.lopez@empresa.com",
        pos_sales_rep, dept_sales, 40000.0, manager_id="emp_003"
    )
    
    # 7. Promoción de empleado
    print_header("5️⃣  PROMOCIÓN DE EMPLEADO")
    print("María González ha demostrado excelente desempeño. ¡Será promovida!\n")
    hr_manager.promote_employee("emp_002", pos_dev_senior)
    
    # 8. Ajuste salarial
    print_header("6️⃣  AJUSTE SALARIAL")
    print("Juan Pérez recibirá un aumento por su buen desempeño.\n")
    hr_manager.adjust_salary("emp_001", 82000.0, "Aumento por desempeño anual")
    
    # 9. Transferencia de departamento
    print_header("7️⃣  TRANSFERENCIA DE DEPARTAMENTO")
    print("Carlos Rodríguez será transferido al departamento de Tecnología.\n")
    hr_manager.transfer_employee("emp_003", dept_it)
    
    # 10. Terminación de contrato
    print_header("8️⃣  TERMINACIÓN DE CONTRATO")
    print("Lamentablemente, Luis López ha decidido renunciar.\n")
    hr_manager.terminate_employee("emp_005", "Renuncia voluntaria")
    
    # 11. Resumen final
    print_header("9️⃣  RESUMEN DEL SISTEMA")
    
    print(f"\n📊 ESTADÍSTICAS GENERALES:")
    print(f"   • Total de empleados registrados: {len(hr_manager.employees)}")
    print(f"   • Empleados activos: {len(hr_manager.get_active_employees())}")
    print(f"   • Departamentos: {len(hr_manager.departments)}")
    print(f"   • Posiciones definidas: {len(hr_manager.positions)}")
    print(f"   • Nómina total: ${payroll_handler.total_payroll:,.2f}")
    
    print("\n📈 EMPLEADOS POR DEPARTAMENTO:")
    for dept_id, dept in hr_manager.departments.items():
        employees = hr_manager.get_employees_by_department(dept_id)
        print(f"   • {dept.name}: {len(employees)} empleados")
        for emp in employees:
            print(f"      - {emp.full_name} ({emp.position.title})")
    
    print("\n📋 EMPLEADOS ACTIVOS:")
    for emp in hr_manager.get_active_employees():
        print(f"   • {emp.full_name}")
        print(f"      Posición: {emp.position.title}")
        print(f"      Departamento: {emp.department.name}")
        print(f"      Salario: ${emp.salary:,.2f}")
        print()
    
    # 12. Mostrar log de auditoría
    audit_handler.print_logs()
    
    # 13. Información sobre el bus de eventos
    print_header("🔟  INFORMACIÓN DEL BUS DE EVENTOS")
    history = event_bus.get_history()
    print(f"Total de eventos procesados: {len(history)}\n")
    print("Tipos de eventos:")
    event_types = {}
    for event in history:
        event_types[event.event_type] = event_types.get(event.event_type, 0) + 1
    
    for event_type, count in sorted(event_types.items()):
        print(f"   • {event_type}: {count} evento(s)")
    
    print_header("✅ DEMOSTRACIÓN COMPLETADA")
    print("Este sistema demuestra cómo la programación orientada a eventos")
    print("permite un diseño desacoplado, escalable y fácil de mantener.")
    print("Cada componente responde a eventos sin conocer a los demás,")
    print("facilitando la extensión y modificación del sistema.\n")


if __name__ == "__main__":
    main()
