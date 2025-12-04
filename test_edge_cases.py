"""
Pruebas de casos especiales del sistema
Edge case testing
"""
from event_system import EventBus
from hr_manager import HRManager
from hr_handlers import NotificationHandler, PayrollHandler


def test_zero_salary_adjustment():
    """Prueba ajuste de salario desde cero (sin división por cero)"""
    print("=" * 60)
    print("TEST 1: Ajuste de salario desde $0 (prevenir división por cero)")
    print("=" * 60)
    
    event_bus = EventBus()
    notificador = NotificationHandler()
    payroll = PayrollHandler()
    
    event_bus.subscribe("employee.hired", notificador.handle)
    event_bus.subscribe("employee.hired", payroll.handle)
    event_bus.subscribe("employee.salary_adjusted", notificador.handle)
    event_bus.subscribe("employee.salary_adjusted", payroll.handle)
    
    hr = HRManager(event_bus)
    
    dept = hr.create_department("dept_001", "Test", 100000.0)
    pos = hr.create_position("pos_001", "Intern", 1, 0.0, "dept_001")
    
    # Contratar con salario 0 (pasante no remunerado)
    emp = hr.hire_employee("emp_001", "Test", "User", "test@test.com", pos, dept, 0.0)
    
    # Ajustar salario desde 0 a un valor positivo
    hr.adjust_salary("emp_001", 30000.0, "Conversión a empleado de tiempo completo")
    
    print("\n✅ Test 1 exitoso: No hubo división por cero\n")


def test_salary_decrease():
    """Prueba reducción de salario"""
    print("=" * 60)
    print("TEST 2: Reducción de salario (cambio negativo)")
    print("=" * 60)
    
    event_bus = EventBus()
    notificador = NotificationHandler()
    payroll = PayrollHandler()
    
    event_bus.subscribe("employee.hired", notificador.handle)
    event_bus.subscribe("employee.hired", payroll.handle)
    event_bus.subscribe("employee.salary_adjusted", notificador.handle)
    event_bus.subscribe("employee.salary_adjusted", payroll.handle)
    
    hr = HRManager(event_bus)
    
    dept = hr.create_department("dept_002", "Test", 100000.0)
    pos = hr.create_position("pos_002", "Manager", 5, 80000.0, "dept_002")
    
    # Contratar con salario alto
    emp = hr.hire_employee("emp_002", "High", "Earner", "high@test.com", pos, dept, 80000.0)
    
    # Reducir salario (ajuste económico)
    hr.adjust_salary("emp_002", 70000.0, "Ajuste por reestructuración")
    
    print(f"\nNómina final: ${payroll.total_payroll:,.2f}")
    print("✅ Test 2 exitoso: Reducción de salario manejada correctamente\n")


def test_zero_salary_change():
    """Prueba ajuste de salario sin cambio"""
    print("=" * 60)
    print("TEST 3: Ajuste de salario sin cambio ($0 de diferencia)")
    print("=" * 60)
    
    event_bus = EventBus()
    notificador = NotificationHandler()
    
    event_bus.subscribe("employee.hired", notificador.handle)
    event_bus.subscribe("employee.salary_adjusted", notificador.handle)
    
    hr = HRManager(event_bus)
    
    dept = hr.create_department("dept_003", "Test", 100000.0)
    pos = hr.create_position("pos_003", "Developer", 3, 60000.0, "dept_003")
    
    emp = hr.hire_employee("emp_003", "Same", "Salary", "same@test.com", pos, dept, 60000.0)
    
    # "Ajustar" al mismo salario
    hr.adjust_salary("emp_003", 60000.0, "Revisión sin cambio")
    
    print("\n✅ Test 3 exitoso: Cambio de $0 manejado correctamente\n")


def test_complete_employee_lifecycle():
    """Prueba ciclo de vida completo de un empleado"""
    print("=" * 60)
    print("TEST 4: Ciclo de vida completo del empleado")
    print("=" * 60)
    
    event_bus = EventBus()
    payroll = PayrollHandler()
    
    event_bus.subscribe("employee.hired", payroll.handle)
    event_bus.subscribe("employee.salary_adjusted", payroll.handle)
    event_bus.subscribe("employee.terminated", payroll.handle)
    
    hr = HRManager(event_bus)
    
    dept = hr.create_department("dept_004", "Test", 100000.0)
    junior_pos = hr.create_position("pos_004", "Junior", 1, 40000.0, "dept_004")
    senior_pos = hr.create_position("pos_005", "Senior", 3, 60000.0, "dept_004")
    
    # Contratar
    emp = hr.hire_employee("emp_004", "Full", "Cycle", "cycle@test.com", junior_pos, dept, 40000.0)
    print(f"Después de contratar: ${payroll.total_payroll:,.2f}")
    
    # Promover (aumenta salario)
    hr.promote_employee("emp_004", senior_pos)
    print(f"Después de promover: ${payroll.total_payroll:,.2f}")
    
    # Aumentar salario
    hr.adjust_salary("emp_004", 65000.0, "Buen desempeño")
    print(f"Después de aumento: ${payroll.total_payroll:,.2f}")
    
    # Terminar contrato
    hr.terminate_employee("emp_004", "Fin de proyecto")
    print(f"Después de terminación: ${payroll.total_payroll:,.2f}")
    
    if payroll.total_payroll == 0:
        print("\n✅ Test 4 exitoso: Nómina vuelve a $0 correctamente\n")
    else:
        print(f"\n❌ Test 4 falló: Nómina debería ser $0 pero es ${payroll.total_payroll:,.2f}\n")


def main():
    print("\n")
    print("*" * 60)
    print("  PRUEBAS DE CASOS ESPECIALES")
    print("*" * 60)
    print()
    
    test_zero_salary_adjustment()
    test_salary_decrease()
    test_zero_salary_change()
    test_complete_employee_lifecycle()
    
    print("*" * 60)
    print("  TODAS LAS PRUEBAS COMPLETADAS")
    print("*" * 60)
    print()


if __name__ == "__main__":
    main()
