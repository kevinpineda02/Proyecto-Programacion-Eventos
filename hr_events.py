"""
Eventos específicos del Sistema de Recursos Humanos
HR-Specific Events
"""
from event_system import Event
from hr_models import Employee, Department, Position
from typing import Optional


class EmployeeHiredEvent(Event):
    """Evento: Un empleado ha sido contratado"""
    
    def __init__(self, employee: Employee):
        super().__init__(
            event_type="employee.hired",
            data={
                "employee_id": employee.id,
                "employee_name": employee.full_name,
                "position": employee.position.title,
                "department": employee.department.name,
                "salary": employee.salary,
                "hire_date": employee.hire_date.isoformat()
            }
        )
        self.employee = employee


class EmployeeTerminatedEvent(Event):
    """Evento: Un empleado ha sido despedido o renunció"""
    
    def __init__(self, employee: Employee, reason: str):
        super().__init__(
            event_type="employee.terminated",
            data={
                "employee_id": employee.id,
                "employee_name": employee.full_name,
                "position": employee.position.title,
                "department": employee.department.name,
                "salary": employee.salary,
                "reason": reason
            }
        )
        self.employee = employee
        self.reason = reason


class EmployeePromotedEvent(Event):
    """Evento: Un empleado ha sido promovido"""
    
    def __init__(self, employee: Employee, old_position: Position, new_position: Position):
        super().__init__(
            event_type="employee.promoted",
            data={
                "employee_id": employee.id,
                "employee_name": employee.full_name,
                "old_position": old_position.title,
                "new_position": new_position.title,
                "old_level": old_position.level,
                "new_level": new_position.level
            }
        )
        self.employee = employee
        self.old_position = old_position
        self.new_position = new_position


class DepartmentChangedEvent(Event):
    """Evento: Un empleado cambió de departamento"""
    
    def __init__(self, employee: Employee, old_department: Department, new_department: Department):
        super().__init__(
            event_type="employee.department_changed",
            data={
                "employee_id": employee.id,
                "employee_name": employee.full_name,
                "old_department": old_department.name,
                "new_department": new_department.name
            }
        )
        self.employee = employee
        self.old_department = old_department
        self.new_department = new_department


class SalaryAdjustedEvent(Event):
    """Evento: El salario de un empleado ha sido ajustado"""
    
    def __init__(self, employee: Employee, old_salary: float, new_salary: float, reason: str = ""):
        change_amount = new_salary - old_salary
        # Evitar división por cero
        change_percentage = ((new_salary - old_salary) / old_salary) * 100 if old_salary != 0 else 0
        
        super().__init__(
            event_type="employee.salary_adjusted",
            data={
                "employee_id": employee.id,
                "employee_name": employee.full_name,
                "old_salary": old_salary,
                "new_salary": new_salary,
                "change_amount": change_amount,
                "change_percentage": change_percentage,
                "reason": reason
            }
        )
        self.employee = employee
        self.old_salary = old_salary
        self.new_salary = new_salary
        self.reason = reason


class DepartmentCreatedEvent(Event):
    """Evento: Se ha creado un nuevo departamento"""
    
    def __init__(self, department: Department):
        super().__init__(
            event_type="department.created",
            data={
                "department_id": department.id,
                "department_name": department.name,
                "budget": department.budget,
                "max_employees": department.max_employees
            }
        )
        self.department = department
