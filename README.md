# FitNet — Plataforma Integral de Gestión Fitness y Entrenamiento

FitNet es una plataforma web desarrollada en **ASP.NET Core MVC (.NET 10)** para la administración integral de gimnasios y centros de entrenamiento físico. El sistema articula cuatro sectores operativos: portal público y comercial, portal de autogestión para socios (con seguimiento de progreso y biblioteca de ejercicios), área de trabajo para entrenadores personales y dashboard administrativo con métricas de gestión.

## Integrantes del grupo

- **Juan Esteban Carreras** - carrerasjuanesteban@gmail.com - [GitHub](https://github.com/CarrerasJuan)
- **Federico Galán** - federico.galan2023@gmail.com - [GitHub](https://github.com/Federico-Galan)

---

## Arquitectura y Stack Tecnológico

* **Backend:** ASP.NET Core MVC (.NET 10) sobre C#.
* **Persistencia y ORM:** Entity Framework Core (`MySql.EntityFrameworkCore 10.0.9`) con gestión estricta de esquema mediante migraciones.
* **Base de Datos:** MySQL / MariaDB (XAMPP).
* **Seguridad e Identidad:**
  * **ASP.NET Core Identity:** gestión de usuarios, contraseñas con hashing seguro y control de acceso basado en roles (`Member`, `Trainer`, `Admin`, `Owner`).
  * **Autenticación Dual:** Cookies seguras para navegación web MVC y tokens **JWT Bearer** para consumo de la API REST.
* **Frontend y Vistas:**
  * Vistas servidor con **Razor**.
  * **Vue.js 3** (Composition API, build global local) y **Axios** para módulos con interacción dinámica y operaciones asíncronas vía AJAX (ej. ABM de ejercicios).
  * **Bootstrap 5** para diseño adaptativo y grillas responsive.
  * **Font Awesome 6 Free** con tipografías y estilos versionados localmente (100% operativo sin conexión a internet).

---

## Modelo de Dominio y Seguridad

El diseño del sistema establece una separación estricta entre la **autorización por roles de seguridad** y el **modelo de membresías comerciales**:

1. **Roles de Seguridad (Identity):**
   * `Member`: socio o cliente del gimnasio.
   * `Trainer`: profesor o entrenador a cargo de seguimiento y rutinas.
   * `Admin`: personal administrativo para gestión de maestros y cuentas.
   * `Owner`: responsable general con acceso a estadísticas globales y métricas del negocio.

2. **Membresías Comerciales (Entidad de negocio):**
   * `Free`: acceso de prueba (**trial de 24 horas** desde el registro) con visualización de perfil y ejercicios públicos.
   * `Gold`: acceso irrestricto a la biblioteca de ejercicios, rutinas generales del gimnasio y registro de progreso.
   * `Premium`: incluye Gold más asignación de entrenador personal, rutinas individualizadas y seguimiento continuo.

3. **Políticas de Seguridad y Acceso:**
   * La validación de membresías y permisos se ejecuta íntegramente en el **servidor**.
   * Paginación y filtrado implementados directamente sobre el motor de base de datos (`Skip/Take` parametrizado).
   * Almacenamiento seguro de archivos multimedia con nombres GUID y validación de extensiones y tipos MIME.

---

## Estructura del Proyecto

```text
FitNet/
├── Controllers/         # Controladores MVC (Home, Account, Exercises, WorkoutPlans, Members, Admin)
├── Api/Controllers/     # Controladores API REST protegidos (Auth, Exercises, Members)
├── Models/              # Entidades del dominio (ApplicationUser, MemberProfile, TrainerProfile, etc.)
├── ViewModels/          # Modelos de vista para formularios e interfaces Razor
├── DTOs/                # Objetos de transferencia de datos para la API
├── Services/            # Lógica de negocio transversal (membresías, almacenamiento, seed)
├── Data/                # ApplicationDbContext, configuraciones Fluent API y migraciones
├── Views/               # Vistas Razor estructuradas por controlador y layout compartido
└── wwwroot/             # Recursos estáticos locales
    ├── css/             # Hojas de estilo propias
    ├── js/vue-apps/     # Aplicaciones Vue 3 modulares e independientes
    ├── lib/             # Librerías locales (Vue, Axios, Bootstrap, Font Awesome)
    └── uploads/         # Archivos subidos en runtime (avatars, progress, exercises)
```

---

## Requisitos y Puesta en Marcha Local

### Prerrequisitos
* **.NET 10 SDK** instalado en el sistema.
* **XAMPP** (o servidor equivalente con Apache y MySQL/MariaDB en puerto 3306).
* Herramienta global de migraciones instalada:
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

### Pasos para levantar la aplicación

1. **Iniciar los servicios:** abrir el panel de control de XAMPP e iniciar el servicio **MySQL**.
2. **Restaurar paquetes y compilar:**
   ```powershell
   dotnet build
   ```
3. **Aplicar migraciones a la base de datos:**
   ```powershell
   dotnet ef database update
   ```
4. **Ejecutar la aplicación:**
   ```powershell
   dotnet run
   ```
5. Acceder a la plataforma desde el navegador web en la dirección indicada por la consola (ej. `https://localhost:5001` o `http://localhost:5000`).
