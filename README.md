Gestión Eficiente de Almacenes
Aplicación web modular para la gestión de almacenes, desarrollada como Proyecto Final del Grado Superior de Desarrollo de Aplicaciones Multiplataforma por Sergio Femia Valle.

🧾 Descripción
Esta solución permite gestionar de forma eficiente el inventario de un almacén, incluyendo operaciones como entradas, salidas, movimientos de bultos, gestión de productos y usuarios con control de acceso basado en roles. La aplicación es adaptable, escalable y organizada bajo una arquitectura cebolla.

🚀 Tecnologías utilizadas
Frontend: HTML, CSS, JavaScript, jQuery, Bootstrap, DataTables
Backend: ASP.NET Core con C#
Base de Datos: SQL Server
ORM: Entity Framework Core
Autenticación: Identity Framework
Entorno de desarrollo: Visual Studio 2022
Control de versiones: Git + GitHub
Tests: NUnit, Moq
🏗️ Arquitectura
La aplicación está estructurada en 4 capas:

Dominio (Domain): Entidades e interfaces genéricas como IRepository y IUnitOfWork
Aplicación (Application): Interfaces y managers con la lógica de negocio
Infraestructura (Infrastructure): Repositorios, migraciones y acceso a datos
Presentación (Web): Interfaz de usuario con Razor Pages y scripts JS
Además, el backend principal puede modularizarse como paquete NuGet para reutilización en futuras soluciones.

📦 Funcionalidades principales
Gestión de productos, categorías y almacenamiento especial
Control de stock con actualización automática
Registro de entradas, salidas y movimientos internos
Gestión de bultos y detalle de bultos
Autenticación y autorización por roles (Administrador, Empleado)
Exportación a Excel en todas las vistas principales
Interfaz intuitiva con ventanas modales, filtros y paginación
📊 Diseño de la base de datos
Modelo normalizado hasta 3FN
Tablas principales: Producto, Bulto, DetalleBulto, Entrada, Salida, Movimiento, Usuario, Rol, EstadoBulto
Script completo de creación incluido: GestionAlmacenSupermercado_Script.sql
🧪 Pruebas
Pruebas unitarias de controladores
Simulación de dependencias con Moq
Validación de respuestas y comportamiento esperado
📂 Estructura del repositorio
OriginalWarehouse/ ├── Domain/ ├── Application/ ├── Infrastructure/ ├── Web/ └── GestionAlmacenSupermercado_Script.sql

🧑‍💻 Ejecución local
Clonar el repositorio
Restaurar paquetes NuGet
Configurar la conexión a SQL Server
Ejecutar migraciones o importar el script de base de datos
Ejecutar el proyecto desde Visual Studio en modo IIS Express
📌 Pendiente o mejoras futuras
Despliegue en IIS o nube (Azure)
Integración con lectores de código de barras
Gestión de proveedores
API REST para integración con ERP
Autenticación multifactor (MFA) y logs de auditoría
📄 Licencia
Este proyecto ha sido desarrollado como parte del módulo de Proyecto del Grado Superior DAM. Uso educativo y demostrativo.
