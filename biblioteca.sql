-- ==========================================================
-- 1. CREACIÓN DE LA BASE DE DATOS
-- ==========================================================
CREATE DATABASE BibliotecaDB;
GO

USE BibliotecaDB;
GO

-- ==========================================================
-- 2. CREACIÓN DE TABLAS
-- ==========================================================

-- Tabla Autores
CREATE TABLE Autores (
    AutorId INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Nacionalidad VARCHAR(50) NOT NULL,
    Activo BIT DEFAULT 1
);
GO

-- Tabla Libros
CREATE TABLE Libros (
    LibroId INT IDENTITY(1,1) PRIMARY KEY,
    Titulo VARCHAR(150) NOT NULL,
    ISBN VARCHAR(20) UNIQUE NOT NULL,
    AutorId INT NOT NULL,
    Ejemplares INT NOT NULL,
    Activo BIT DEFAULT 1,
    CONSTRAINT FK_Libros_Autores FOREIGN KEY (AutorId) REFERENCES Autores(AutorId)
);
GO

-- Tabla Socios
CREATE TABLE Socios (
    SocioId INT IDENTITY(1,1) PRIMARY KEY,
    DNI VARCHAR(15) UNIQUE NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Activo BIT DEFAULT 1
);
GO

-- Tabla Prestamos
CREATE TABLE Prestamos (
    PrestamoId INT IDENTITY(1,1) PRIMARY KEY,
    SocioId INT NOT NULL,
    FechaPrestamo DATE NOT NULL,
    FechaLimite DATE NOT NULL,
    Estado VARCHAR(50) NOT NULL,
    CONSTRAINT FK_Prestamos_Socios FOREIGN KEY (SocioId) REFERENCES Socios(SocioId)
);
GO

-- Tabla DetallePrestamo (Clave primaria compuesta)
CREATE TABLE DetallePrestamo (
    PrestamoId INT NOT NULL,
    LibroId INT NOT NULL,
    FechaDevolucion DATE NULL,
    CONSTRAINT PK_DetallePrestamo PRIMARY KEY (PrestamoId, LibroId),
    CONSTRAINT FK_DetallePrestamo_Prestamos FOREIGN KEY (PrestamoId) REFERENCES Prestamos(PrestamoId),
    CONSTRAINT FK_DetallePrestamo_Libros FOREIGN KEY (LibroId) REFERENCES Libros(LibroId)
);
GO

-- ==========================================================
-- 3. INSERCIÓN DE DATOS DE PRUEBA
-- ==========================================================

-- 8 Autores
INSERT INTO Autores (Nombre, Nacionalidad) VALUES
('Gabriel García Márquez', 'Colombiana'),
('Mario Vargas Llosa', 'Peruana'),
('Isabel Allende', 'Chilena'),
('Jorge Luis Borges', 'Argentina'),
('Julio Cortázar', 'Argentina'),
('Pablo Neruda', 'Chilena'),
('Laura Esquivel', 'Mexicana'),
('Ernesto Sábato', 'Argentina');
GO

-- 20 Libros
INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares) VALUES
('Cien años de soledad', '978-0307474728', 1, 5),
('El coronel no tiene quien le escriba', '978-8437604909', 1, 3),
('Crónica de una muerte anunciada', '978-1400034956', 1, 4),
('La ciudad y los perros', '978-8466330923', 2, 4),
('La casa verde', '978-8420471839', 2, 2),
('Conversación en La Catedral', '978-8437604343', 2, 3),
('La casa de los espíritus', '978-1501117015', 3, 5),
('Paula', '978-0060927233', 3, 3),
('Ficciones', '978-0307950932', 4, 3),
('El Aleph', '978-8420633114', 4, 4),
('Rayuela', '978-8437605739', 5, 4),
('Bestiario', '978-8420471556', 5, 2),
('Veinte poemas de amor y una canción desesperada', '978-9561214040', 6, 6),
('Cien sonetos de amor', '978-8481093391', 6, 3),
('Como agua para chocolate', '978-0385420174', 7, 5),
('La ley del amor', '978-8401326448', 7, 2),
('El túnel', '978-8432201554', 8, 3),
('Sobre héroes y tumbas', '978-8432201561', 8, 2),
('El otoño del patriarca', '978-8437603179', 1, 2),
('Los cachorros', '978-8466318990', 2, 3);
GO

-- 10 Socios
INSERT INTO Socios (DNI, Nombre, Email) VALUES
('71234567', 'Carlos Pérez', 'carlos.perez@email.com'),
('72345678', 'Ana Gómez', 'ana.gomez@email.com'),
('73456789', 'Luis Torres', 'luis.torres@email.com'),
('74567890', 'María Rodríguez', 'maria.rodriguez@email.com'),
('75678901', 'Jorge Quispe', 'jorge.quispe@email.com'),
('76789012', 'Lucía Mendoza', 'lucia.mendoza@email.com'),
('77890123', 'Pedro Castillo', 'pedro.castillo@email.com'),
('78901234', 'Sofía Huamán', 'sofia.huaman@email.com'),
('79012345', 'Miguel Rojas', 'miguel.rojas@email.com'),
('80123456', 'Elena Flores', 'elena.flores@email.com');
GO

-- 5 Préstamos y sus Detalles
-- Préstamo 1: Socio 1 (Carlos Pérez) con 3 libros pendientes (FechaDevolucion en NULL)
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
(1, '2026-09-01', '2026-09-15', 'Pendiente');

INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
(1, 1, NULL),
(1, 4, NULL),
(1, 7, NULL);

-- Préstamo 2: Socio 2 (Ana Gómez) - Devuelto
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
(2, '2026-08-10', '2026-08-24', 'Devuelto');

INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
(2, 9, '2026-08-20');

-- Préstamo 3: Socio 3 (Luis Torres) - Devuelto
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
(3, '2026-08-15', '2026-08-29', 'Devuelto');

INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
(3, 11, '2026-08-25');

-- Préstamo 4: Socio 4 (María Rodríguez) - Pendiente 1 libro
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
(4, '2026-09-10', '2026-09-24', 'Pendiente');

INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
(4, 15, NULL);

-- Préstamo 5: Socio 5 (Jorge Quispe) - Devuelto
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
(5, '2026-08-01', '2026-08-15', 'Devuelto');

INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
(5, 17, '2026-08-10');
GO