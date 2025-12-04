"""
Modelos de Dominio para el Sistema de Recursos Humanos
HR Domain Models
"""
from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional
from enum import Enum


class EmployeeStatus(Enum):
    """Estados posibles de un empleado"""
    ACTIVE = "active"
    INACTIVE = "inactive"
    ON_LEAVE = "on_leave"
    TERMINATED = "terminated"


@dataclass
class Department:
    """Modelo de Departamento"""
    id: str
    name: str
    budget: float
    manager_id: Optional[str] = None
    max_employees: int = 50
    
    def __repr__(self):
        return f"Department({self.name}, ID: {self.id})"


@dataclass
class Position:
    """Modelo de Posición/Cargo"""
    id: str
    title: str
    level: int
    base_salary: float
    department_id: str
    
    def __repr__(self):
        return f"Position({self.title}, Level: {self.level})"


@dataclass
class Employee:
    """Modelo de Empleado"""
    id: str
    first_name: str
    last_name: str
    email: str
    position: Position
    department: Department
    salary: float
    hire_date: datetime = field(default_factory=datetime.now)
    status: EmployeeStatus = EmployeeStatus.ACTIVE
    manager_id: Optional[str] = None
    
    @property
    def full_name(self) -> str:
        return f"{self.first_name} {self.last_name}"
    
    def __repr__(self):
        return f"Employee({self.full_name}, {self.position.title}, Status: {self.status.value})"
