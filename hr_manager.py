"""
Sistema de Gestión de Recursos Humanos Orientado a Eventos
Event-Driven HR Management System
"""
from event_system import EventBus
from hr_models import Employee, Department, Position, EmployeeStatus
from hr_events import (
    EmployeeHiredEvent,
    EmployeeTerminatedEvent,
    EmployeePromotedEvent,
    DepartmentChangedEvent,
    SalaryAdjustedEvent,
    DepartmentCreatedEvent
)
from typing import Dict, List, Optional


class HRManager:
    """Gestor principal de Recursos Humanos que coordina todas las operaciones"""
    
    def __init__(self, event_bus: EventBus):
        self.event_bus = event_bus
        self.employees: Dict[str, Employee] = {}
        self.departments: Dict[str, Department] = {}
        self.positions: Dict[str, Position] = {}
    
    # ===== GESTIÓN DE DEPARTAMENTOS =====
    
    def create_department(self, dept_id: str, name: str, budget: float, max_employees: int = 50) -> Department:
        """Crea un nuevo departamento"""
        department = Department(
            id=dept_id,
            name=name,
            budget=budget,
            max_employees=max_employees
        )
        self.departments[dept_id] = department
        
        # Publicar evento
        event = DepartmentCreatedEvent(department)
        self.event_bus.publish(event)
        
        return department
    
    def get_department(self, dept_id: str) -> Optional[Department]:
        """Obtiene un departamento por ID"""
        return self.departments.get(dept_id)
    
    # ===== GESTIÓN DE POSICIONES =====
    
    def create_position(self, pos_id: str, title: str, level: int, 
                       base_salary: float, department_id: str) -> Position:
        """Crea una nueva posición"""
        position = Position(
            id=pos_id,
            title=title,
            level=level,
            base_salary=base_salary,
            department_id=department_id
        )
        self.positions[pos_id] = position
        return position
    
    def get_position(self, pos_id: str) -> Optional[Position]:
        """Obtiene una posición por ID"""
        return self.positions.get(pos_id)
    
    # ===== GESTIÓN DE EMPLEADOS =====
    
    def hire_employee(self, emp_id: str, first_name: str, last_name: str, 
                     email: str, position: Position, department: Department, 
                     salary: float, manager_id: Optional[str] = None) -> Employee:
        """Contrata un nuevo empleado"""
        employee = Employee(
            id=emp_id,
            first_name=first_name,
            last_name=last_name,
            email=email,
            position=position,
            department=department,
            salary=salary,
            manager_id=manager_id
        )
        self.employees[emp_id] = employee
        
        # Publicar evento
        event = EmployeeHiredEvent(employee)
        self.event_bus.publish(event)
        
        return employee
    
    def terminate_employee(self, emp_id: str, reason: str) -> None:
        """Termina el contrato de un empleado"""
        employee = self.employees.get(emp_id)
        if not employee:
            raise ValueError(f"Empleado {emp_id} no encontrado")
        
        employee.status = EmployeeStatus.TERMINATED
        
        # Publicar evento
        event = EmployeeTerminatedEvent(employee, reason)
        self.event_bus.publish(event)
    
    def promote_employee(self, emp_id: str, new_position: Position) -> None:
        """Promueve a un empleado a una nueva posición"""
        employee = self.employees.get(emp_id)
        if not employee:
            raise ValueError(f"Empleado {emp_id} no encontrado")
        
        old_position = employee.position
        employee.position = new_position
        
        # Ajustar salario al de la nueva posición (si es mayor)
        if new_position.base_salary > employee.salary:
            old_salary = employee.salary
            employee.salary = new_position.base_salary
            # También publicar evento de ajuste salarial
            salary_event = SalaryAdjustedEvent(employee, old_salary, employee.salary, "Promoción")
            self.event_bus.publish(salary_event)
        
        # Publicar evento de promoción
        event = EmployeePromotedEvent(employee, old_position, new_position)
        self.event_bus.publish(event)
    
    def transfer_employee(self, emp_id: str, new_department: Department) -> None:
        """Transfiere a un empleado a un nuevo departamento"""
        employee = self.employees.get(emp_id)
        if not employee:
            raise ValueError(f"Empleado {emp_id} no encontrado")
        
        old_department = employee.department
        employee.department = new_department
        
        # Publicar evento
        event = DepartmentChangedEvent(employee, old_department, new_department)
        self.event_bus.publish(event)
    
    def adjust_salary(self, emp_id: str, new_salary: float, reason: str = "") -> None:
        """Ajusta el salario de un empleado"""
        employee = self.employees.get(emp_id)
        if not employee:
            raise ValueError(f"Empleado {emp_id} no encontrado")
        
        old_salary = employee.salary
        employee.salary = new_salary
        
        # Publicar evento
        event = SalaryAdjustedEvent(employee, old_salary, new_salary, reason)
        self.event_bus.publish(event)
    
    def get_employee(self, emp_id: str) -> Optional[Employee]:
        """Obtiene un empleado por ID"""
        return self.employees.get(emp_id)
    
    def get_all_employees(self) -> List[Employee]:
        """Obtiene todos los empleados"""
        return list(self.employees.values())
    
    def get_employees_by_department(self, dept_id: str) -> List[Employee]:
        """Obtiene todos los empleados de un departamento"""
        return [emp for emp in self.employees.values() 
                if emp.department.id == dept_id and emp.status == EmployeeStatus.ACTIVE]
    
    def get_active_employees(self) -> List[Employee]:
        """Obtiene todos los empleados activos"""
        return [emp for emp in self.employees.values() 
                if emp.status == EmployeeStatus.ACTIVE]
