"""
Manejadores de Eventos para el Sistema de Recursos Humanos
HR Event Handlers
"""
from event_system import Event, EventHandler
from typing import List
from datetime import datetime


class NotificationHandler(EventHandler):
    """Manejador que envía notificaciones cuando ocurren eventos"""
    
    def __init__(self, name: str = "Sistema de Notificaciones"):
        self.name = name
        self.notifications: List[str] = []
    
    def handle(self, event: Event) -> None:
        """Maneja eventos enviando notificaciones"""
        message = self._create_notification_message(event)
        self.notifications.append(message)
        print(f"  📧 {self.name}: {message}")
    
    def _create_notification_message(self, event: Event) -> str:
        """Crea un mensaje de notificación basado en el tipo de evento"""
        event_type = event.event_type
        data = event.data
        
        if event_type == "employee.hired":
            return f"¡Bienvenido {data['employee_name']} al departamento de {data['department']} como {data['position']}!"
        
        elif event_type == "employee.terminated":
            return f"{data['employee_name']} ha dejado la empresa. Motivo: {data['reason']}"
        
        elif event_type == "employee.promoted":
            return f"¡Felicidades! {data['employee_name']} ha sido promovido de {data['old_position']} a {data['new_position']}"
        
        elif event_type == "employee.department_changed":
            return f"{data['employee_name']} se ha trasladado de {data['old_department']} a {data['new_department']}"
        
        elif event_type == "employee.salary_adjusted":
            change_type = "aumento" if data['change_amount'] > 0 else "reducción"
            return f"Ajuste salarial para {data['employee_name']}: ${data['old_salary']:,.2f} → ${data['new_salary']:,.2f} ({change_type} del {abs(data['change_percentage']):.1f}%)"
        
        elif event_type == "department.created":
            return f"Nuevo departamento creado: {data['department_name']} (Presupuesto: ${data['budget']:,.2f})"
        
        else:
            return f"Evento: {event_type}"


class AuditLogHandler(EventHandler):
    """Manejador que registra todos los eventos en un log de auditoría"""
    
    def __init__(self):
        self.audit_log: List[dict] = []
    
    def handle(self, event: Event) -> None:
        """Registra el evento en el log de auditoría"""
        log_entry = {
            "timestamp": event.timestamp.isoformat(),
            "event_type": event.event_type,
            "data": event.data
        }
        self.audit_log.append(log_entry)
        print(f"  📝 Auditoría: Evento registrado en el log")
    
    def get_logs(self) -> List[dict]:
        """Obtiene todos los registros de auditoría"""
        return self.audit_log.copy()
    
    def print_logs(self) -> None:
        """Imprime todos los logs de auditoría"""
        print("\n" + "="*80)
        print("LOG DE AUDITORÍA")
        print("="*80)
        for idx, log in enumerate(self.audit_log, 1):
            print(f"\n{idx}. [{log['timestamp']}] {log['event_type']}")
            for key, value in log['data'].items():
                print(f"   - {key}: {value}")


class DepartmentCapacityHandler(EventHandler):
    """Manejador que controla la capacidad de empleados por departamento"""
    
    def __init__(self):
        self.department_counts = {}
    
    def handle(self, event: Event) -> None:
        """Controla la capacidad del departamento"""
        event_type = event.event_type
        data = event.data
        
        if event_type == "employee.hired":
            dept = data['department']
            self.department_counts[dept] = self.department_counts.get(dept, 0) + 1
            print(f"  👥 Capacidad: {dept} ahora tiene {self.department_counts[dept]} empleados")
        
        elif event_type == "employee.terminated":
            dept = data['department']
            if dept in self.department_counts:
                self.department_counts[dept] = max(0, self.department_counts[dept] - 1)
                print(f"  👥 Capacidad: {dept} ahora tiene {self.department_counts[dept]} empleados")
        
        elif event_type == "employee.department_changed":
            old_dept = data['old_department']
            new_dept = data['new_department']
            if old_dept in self.department_counts:
                self.department_counts[old_dept] = max(0, self.department_counts[old_dept] - 1)
            self.department_counts[new_dept] = self.department_counts.get(new_dept, 0) + 1
            print(f"  👥 Capacidad: {old_dept} → {self.department_counts.get(old_dept, 0)}, {new_dept} → {self.department_counts[new_dept]}")


class PayrollHandler(EventHandler):
    """Manejador que calcula y rastrea la nómina total"""
    
    def __init__(self):
        self.total_payroll = 0.0
    
    def handle(self, event: Event) -> None:
        """Actualiza cálculos de nómina basados en eventos"""
        event_type = event.event_type
        data = event.data
        
        if event_type == "employee.hired":
            self.total_payroll += data['salary']
            print(f"  💰 Nómina: +${data['salary']:,.2f}. Total: ${self.total_payroll:,.2f}")
        
        elif event_type == "employee.terminated":
            self.total_payroll -= data['salary']
            print(f"  💰 Nómina: -${data['salary']:,.2f}. Total: ${self.total_payroll:,.2f}")
        
        elif event_type == "employee.salary_adjusted":
            change = data['change_amount']
            self.total_payroll += change
            change_sign = "+" if change >= 0 else ""
            print(f"  💰 Nómina: {change_sign}${change:,.2f}. Total: ${self.total_payroll:,.2f}")
