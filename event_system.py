"""
Sistema de Eventos Base para Programación Orientada a Eventos
Base Event System for Event-Driven Programming
"""
from datetime import datetime
from typing import Callable, Dict, List, Any
from abc import ABC, abstractmethod


class Event:
    """Clase base para todos los eventos del sistema"""
    
    def __init__(self, event_type: str, data: Dict[str, Any] = None):
        self.event_type = event_type
        self.data = data or {}
        self.timestamp = datetime.now()
    
    def __repr__(self):
        return f"Event(type={self.event_type}, timestamp={self.timestamp}, data={self.data})"


class EventHandler(ABC):
    """Interfaz para manejadores de eventos"""
    
    @abstractmethod
    def handle(self, event: Event) -> None:
        """Maneja un evento específico"""
        pass


class EventBus:
    """Bus de eventos para gestionar suscripciones y despacho de eventos"""
    
    def __init__(self):
        self._subscribers: Dict[str, List[Callable[[Event], None]]] = {}
        self._event_history: List[Event] = []
    
    def subscribe(self, event_type: str, handler: Callable[[Event], None]) -> None:
        """Suscribe un manejador a un tipo de evento específico"""
        if event_type not in self._subscribers:
            self._subscribers[event_type] = []
        self._subscribers[event_type].append(handler)
        print(f"✓ Handler suscrito al evento: {event_type}")
    
    def unsubscribe(self, event_type: str, handler: Callable[[Event], None]) -> None:
        """Cancela la suscripción de un manejador"""
        if event_type in self._subscribers:
            self._subscribers[event_type].remove(handler)
    
    def publish(self, event: Event) -> None:
        """Publica un evento a todos los suscriptores"""
        self._event_history.append(event)
        print(f"\n📢 Evento publicado: {event.event_type}")
        
        if event.event_type in self._subscribers:
            for handler in self._subscribers[event.event_type]:
                try:
                    handler(event)
                except Exception as e:
                    print(f"❌ Error al procesar evento {event.event_type}: {e}")
    
    def get_history(self) -> List[Event]:
        """Obtiene el historial completo de eventos"""
        return self._event_history.copy()
    
    def clear_history(self) -> None:
        """Limpia el historial de eventos"""
        self._event_history.clear()
