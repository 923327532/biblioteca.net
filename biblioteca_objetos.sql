-- ==========================================================
-- 4. OBJETOS DE BASE DE DATOS PARA LA CAPA DE DATOS
-- Ejecutar sobre BibliotecaDB (después de crear tablas y datos).
-- ==========================================================
USE BibliotecaDB;
GO

-- Tipo de tabla para enviar el detalle del préstamo como parámetro con valor de tabla.
IF TYPE_ID('dbo.DetallePrestamoTipo') IS NULL
BEGIN
    CREATE TYPE dbo.DetallePrestamoTipo AS TABLE
    (
        LibroId INT NOT NULL PRIMARY KEY,
        FechaDevolucion DATE NULL
    );
END
GO

-- Función: libros pendientes de un socio.
CREATE OR ALTER FUNCTION dbo.fn_LibrosPendientesSocio(@SocioId INT)
RETURNS INT
AS
BEGIN
    DECLARE @Total INT;

    SELECT @Total = COUNT(1)
    FROM DetallePrestamo d
    INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
    WHERE p.SocioId = @SocioId AND d.FechaDevolucion IS NULL;

    RETURN ISNULL(@Total, 0);
END
GO

-- Procedimiento: registrar préstamo (cabecera + detalle + descuento de stock).
CREATE OR ALTER PROCEDURE dbo.usp_RegistrarPrestamo
    @SocioId INT,
    @FechaPrestamo DATE,
    @FechaLimite DATE,
    @Detalle dbo.DetallePrestamoTipo READONLY,
    @PrestamoId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
        VALUES (@SocioId, @FechaPrestamo, @FechaLimite, 'Pendiente');

        SET @PrestamoId = CAST(SCOPE_IDENTITY() AS INT);

        INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion)
        SELECT @PrestamoId, LibroId, FechaDevolucion
        FROM @Detalle;

        UPDATE l
        SET l.Ejemplares = l.Ejemplares - 1
        FROM Libros l
        INNER JOIN @Detalle d ON d.LibroId = l.LibroId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Procedimiento: devolver el ejemplar al stock y actualizar el estado del préstamo.
-- @LibroId indica el ejemplar que se acaba de devolver; @FechaDevolucion es la
-- fecha registrada en el detalle (permite devoluciones con retraso).
CREATE OR ALTER PROCEDURE dbo.usp_ActualizarEstadoPrestamo
    @PrestamoId INT,
    @LibroId INT = NULL,
    @FechaDevolucion DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Devolver al stock el ejemplar recién devuelto (por libro y fecha exactos,
        -- no con GETDATE(), porque la devolución puede registrarse con retraso).
        UPDATE l
        SET l.Ejemplares = l.Ejemplares + 1
        FROM Libros l
        WHERE l.LibroId = @LibroId
          AND EXISTS (SELECT 1
                      FROM DetallePrestamo d
                      WHERE d.PrestamoId = @PrestamoId
                        AND d.LibroId = @LibroId
                        AND d.FechaDevolucion = @FechaDevolucion
                        AND d.FechaDevolucion IS NOT NULL);

        -- Cuando no quedan libros pendientes, el préstamo pasa a Devuelto.
        IF NOT EXISTS (SELECT 1
                       FROM DetallePrestamo
                       WHERE PrestamoId = @PrestamoId AND FechaDevolucion IS NULL)
        BEGIN
            UPDATE Prestamos
            SET Estado = 'Devuelto'
            WHERE PrestamoId = @PrestamoId;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
