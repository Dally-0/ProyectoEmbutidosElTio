# 🥩 Embutidos El Tío — Sistema Web de Gestión y Comercio Electrónico

> Plataforma web full-stack desarrollada con **ASP.NET Core 9 MVC** para la gestión integral de una empresa distribuidora de embutidos, incluyendo catálogo de productos, carrito de compras, pagos en línea y panel de administración.

---

## 📋 Tabla de Contenidos

- [Descripción General](#-descripción-general)
- [Stack Tecnológico](#-stack-tecnológico)
- [Arquitectura del Proyecto](#-arquitectura-del-proyecto)
- [Funcionalidades Principales](#-funcionalidades-principales)
- [Modelos de Base de Datos](#-modelos-de-base-de-datos)
- [Integraciones de Pago](#-integraciones-de-pago)
- [Seguridad y Autenticación](#-seguridad-y-autenticación)
- [Estructura de Carpetas](#-estructura-de-carpetas)
- [Configuración y Puesta en Marcha](#-configuración-y-puesta-en-marcha)
- [Credenciales por Defecto](#-credenciales-por-defecto)

---

## 📖 Descripción General

**Embutidos El Tío** es una aplicación web empresarial completa que digitaliza los procesos de venta y administración de una empresa de embutidos. El sistema cuenta con dos perfiles de usuario diferenciados:

- **Cliente**: Explorar el catálogo, agregar productos al carrito y realizar pagos en línea mediante PayPal o Stripe.
- **Administrador**: Gestionar productos, inventario, pedidos, usuarios y publicar noticias desde un dashboard centralizado con métricas en tiempo real.

---

## 🛠️ Stack Tecnológico

### Backend

| Tecnología | Versión | Uso |
|---|---|---|
| **ASP.NET Core MVC** | .NET 9.0 | Framework principal — patrón MVC |
| **Entity Framework Core** | 9.0.0 | ORM para acceso a base de datos |
| **SQL Server** | — | Motor de base de datos relacional |
| **C#** | 13 | Lenguaje principal del servidor |
| **Cookie Authentication** | — | Manejo de sesiones y seguridad |

### Frontend

| Tecnología | Versión | Uso |
|---|---|---|
| **Razor Views (.cshtml)** | — | Plantillas del lado del servidor |
| **Tailwind CSS** | 3.4.1 | Framework de estilos utilitario |
| **JavaScript** | ES6+ | Interactividad en el cliente |

### Servicios Externos

| Servicio | SDK/API | Uso |
|---|---|---|
| **Stripe** | `Stripe.net` v50.3.0 | Procesamiento de pagos con tarjeta |
| **PayPal** | REST API v2 | Pagos alternativos vía OAuth2 |

---

## 🏗️ Arquitectura del Proyecto

El proyecto sigue el patrón **MVC (Model-View-Controller)** de ASP.NET Core con una capa de servicios para lógica de negocio:

```
┌─────────────────────────────────────────────────────────┐
│                      CLIENTE / BROWSER                   │
└───────────────────────────┬─────────────────────────────┘
                            │ HTTP Request
┌───────────────────────────▼─────────────────────────────┐
│                    ASP.NET Core Pipeline                  │
│  Authentication → Authorization → Routing → Session      │
└───────────────────────────┬─────────────────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         ▼                  ▼                  ▼
   Controllers          Services          Middleware
   (11 archivos)   (PayPal, Stripe)     (Auth, Session)
         │                  │
         ▼                  ▼
      Models ──────► AppDbContext (EF Core)
                            │
                            ▼
                     SQL Server Database
```

### Capas del Sistema

- **Controllers** — Orquestan el flujo de datos entre vista y modelo
- **Models** — Representan entidades de negocio con anotaciones de validación
- **Views** — Razor templates con Tailwind CSS para la UI
- **Data** — `AppDbContext` configurado con EF Core
- **Services** — Lógica de integración con APIs externas (PayPal, Stripe)

---

## ⚡ Funcionalidades Principales

### 👤 Módulo de Clientes
- Registro e inicio de sesión con hash SHA-256
- Catálogo de productos con filtros por categoría
- Carrito de compras con manejo de sesión
- Proceso de checkout con **múltiples métodos de pago**
- Historial de pedidos personales

### 🛡️ Panel de Administración
- **Dashboard** con KPIs en tiempo real: total de pedidos, usuarios, productos y noticias
- Vista de últimos 5 pedidos recientes
- **Gestión de Inventario** con filtros inteligentes:
  - Productos vencidos
  - Productos por vencer en 30 días
  - Stock bajo (configurable por producto)
- **Gestión de Pagos** con vista unificada de PayPal, Stripe y efectivo
- Cálculo automático de ganancia (ingresos − costo de producción)
- CRUD completo de Productos, Usuarios, Pedidos y Noticias

### 📰 Módulo de Noticias
- Publicación y gestión de noticias vinculadas al usuario administrador
- Vista pública accesible sin autenticación

---

## 🗄️ Modelos de Base de Datos

El sistema cuenta con **11 entidades** mapeadas con Entity Framework Core Code-First:

```
Roles ──────────────────┐
                        │
EstadoUsuario           ├── Usuario ──────────────────────┐
                        │                                  │
Categorias ─────────────┤                                  │
                        │                                  ▼
                        ├── Producto           Pedido ─────────── DetallePedido
                        │      │                  │
EstadoPedido ───────────┘      │                  ├── PagoPaypal
                               │                  └── PagoStripe
                               └── Carrito (Session)

Noticias ─── Usuario (Admin)
```

### Entidades Clave

| Entidad | Descripción | Campos Destacados |
|---|---|---|
| `Usuario` | Clientes y administradores | `PasswordHash (byte[])`, `IdRol`, `Activo` |
| `Producto` | Catálogo de embutidos | `PrecioProduccion`, `Precio_final`, `StockMinimo`, `FechaVencimiento` |
| `Pedido` | Órdenes de compra | `Total (decimal 18,2)`, `IdEstadoPedido` |
| `DetallePedido` | Líneas de cada pedido | `Cantidad`, `PrecioUnitario` |
| `PagoPaypal` | Transacciones PayPal | `IdTransaccionPaypal`, `EstadoPago` |
| `PagoStripe` | Transacciones Stripe | `IdTransaccionStripe`, `MontoPagado` |
| `Noticia` | Publicaciones del negocio | `TextoNoticia`, `FechaPublicacion` |

---

## 💳 Integraciones de Pago

### PayPal REST API v2

Implementación directa con `HttpClient` sin SDK de terceros, utilizando el flujo **OAuth2 client credentials**:

1. Solicitud de `access_token` vía `POST /v1/oauth2/token`
2. Creación de orden con `POST /v2/checkout/orders` (intención `CAPTURE`)
3. Captura del pago con `POST /v2/checkout/orders/{id}/capture`
4. Soporte para modo **Sandbox** y **Live** configurable en `appsettings.json`

### Stripe

Integrado mediante el SDK oficial `Stripe.net` v50.3.0, procesando pagos con tarjeta directamente desde el servidor.

---

## 🔐 Seguridad y Autenticación

- **Contraseñas**: Almacenadas como hash `SHA-256` en la base de datos (campo `byte[]`)
- **Sesiones**: Cookie-based Authentication con `HttpOnly` y `IsEssential = true`
- **Tiempo de sesión**: 30 minutos de inactividad
- **Autorización por roles**: Decorador `[Authorize(Roles = "Administrador")]` en todos los controladores del panel administrativo
- **Rutas protegidas**: Redirección automática a `/Account/Login` para recursos no autorizados
- **Database Seeder**: Inicialización automática de roles y usuario administrador por defecto al arrancar la aplicación

---

## 📁 Estructura de Carpetas

```
ProyectoEmbutidosElTio/
├── ProyectoFinalEmbutidosElTio.sln
└── ProyectoFinalEmbutidosElTio/
    ├── Controllers/
    │   ├── AccountController.cs        # Login, registro, logout
    │   ├── CarritoController.cs        # Carrito + checkout + pagos
    │   ├── DashboardController.cs      # Panel admin (KPIs, inventario, pagos)
    │   ├── TiendaController.cs         # Catálogo público
    │   ├── AdminProductosController.cs # CRUD de productos
    │   ├── AdminPedidosController.cs   # Gestión de pedidos
    │   ├── AdminUsuariosController.cs  # Gestión de usuarios
    │   ├── AdminNoticiasController.cs  # Gestión de noticias
    │   ├── NoticiasController.cs       # Vista pública de noticias
    │   ├── ClienteController.cs        # Perfil del cliente
    │   └── HomeController.cs           # Página principal
    ├── Models/
    │   ├── MainModels.cs               # Entidades principales (Usuario, Producto, Pedido...)
    │   ├── MasterModels.cs             # Entidades de catálogo (Rol, Categoria, Estado...)
    │   ├── PagoStripe.cs               # Modelo de pago Stripe
    │   ├── PayPal/                     # Modelos de solicitud/respuesta PayPal
    │   └── ViewModels/                 # DTOs para las vistas
    ├── Data/
    │   └── AppDbContext.cs             # Contexto de EF Core con configuración Fluent API
    ├── Services/
    │   ├── PayPalService.cs            # Integración REST API PayPal
    │   └── StripeService.cs            # Integración SDK Stripe
    ├── Views/                          # Vistas Razor organizadas por controlador
    │   ├── Shared/                     # Layout principal y parciales
    │   └── ...
    ├── Styles/
    │   └── input.css                   # Entrada de Tailwind CSS
    ├── wwwroot/                        # Archivos estáticos (CSS compilado, JS, imágenes)
    ├── tailwind.config.js              # Configuración de colores y temas
    ├── Program.cs                      # Entry point — DI, middleware, seeder
    └── appsettings.json                # Configuración de conexión y APIs
```

---

## ⚙️ Configuración y Puesta en Marcha

### Prerrequisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) (Express o superior)
- [Node.js](https://nodejs.org/) (para compilar Tailwind CSS)
- Cuentas de [PayPal Developer](https://developer.paypal.com/) y [Stripe](https://stripe.com/) (opcional para pagos)

### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/ProyectoEmbutidosElTio.git
cd ProyectoEmbutidosElTio
```

### 2. Configurar la cadena de conexión

Edita `appsettings.json` con los datos de tu servidor SQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=EmbutidosElTio;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "PayPal": {
    "ClientId": "TU_CLIENT_ID",
    "Secret": "TU_SECRET",
    "Mode": "Sandbox"
  },
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_..."
  }
}
```

### 3. Instalar dependencias y compilar CSS

```bash
cd ProyectoFinalEmbutidosElTio
npm install
npm run build:css
```

> 💡 Para desarrollo con recarga automática de estilos: `npm run watch:css`

### 4. Ejecutar la aplicación

```bash
dotnet run
```

La base de datos se crea y semilla automáticamente en el primer arranque gracias al **Database Seeder** integrado en `Program.cs`.

---

## 🔑 Credenciales por Defecto

El sistema crea automáticamente un usuario administrador al iniciar por primera vez:

| Campo | Valor |
|---|---|
| **Correo** | `admin@eltio.com` |
| **Contraseña** | `Admin123!` |
| **Rol** | Administrador |

> ⚠️ **Importante**: Cambia estas credenciales en un entorno de producción.

---

## 🎨 Paleta de Colores

El proyecto usa una paleta personalizada de tonos anaranjados definida en `tailwind.config.js`, representando la identidad visual de la marca:

| Token | Hex | Uso |
|---|---|---|
| `primary-500` | `#f97316` | Color principal de la marca |
| `primary-700` | `#c2410c` | Hover, estados activos |
| `primary-900` | `#7c2d12` | Textos oscuros, fondos |

---

## 👥 Autores

Proyecto académico desarrollado como trabajo final de curso.

---

*Desarrollado con ❤️ usando ASP.NET Core 9 · Entity Framework Core · Tailwind CSS · Stripe · PayPal REST API*
