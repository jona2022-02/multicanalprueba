/*
  Multicanal — configuración por canal
  sqlcmd -S .\SQLEXPRESS -E -i 01_configuracion_canal.sql
*/

IF DB_ID(N'Multicanal') IS NULL
BEGIN
    CREATE DATABASE Multicanal;
END
GO

USE Multicanal;
GO

IF OBJECT_ID(N'dbo.ConfiguracionCanal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfiguracionCanal
    (
        Canal              VARCHAR(10)   NOT NULL CONSTRAINT PK_ConfiguracionCanal PRIMARY KEY,
        UrlServicio        NVARCHAR(500) NOT NULL,
        ApiKey             NVARCHAR(100) NOT NULL,
        MostrarMenu        BIT           NOT NULL CONSTRAINT DF_ConfiguracionCanal_MostrarMenu DEFAULT (1),
        PaginaDestino      NVARCHAR(200) NOT NULL,
        TimeoutSegundos    INT           NOT NULL CONSTRAINT DF_ConfiguracionCanal_Timeout DEFAULT (5),
        Activo             BIT           NOT NULL CONSTRAINT DF_ConfiguracionCanal_Activo DEFAULT (1),
        FechaCreacion      DATETIME2(0)  NOT NULL CONSTRAINT DF_ConfiguracionCanal_Fecha DEFAULT (SYSUTCDATETIME())
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConfiguracionCanal_Obtener
    @Canal VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.Canal,
        c.UrlServicio,
        c.ApiKey,
        c.MostrarMenu,
        c.PaginaDestino,
        c.TimeoutSegundos
    FROM dbo.ConfiguracionCanal AS c
    WHERE c.Canal = @Canal
      AND c.Activo = 1;
END
GO

/*
  UrlServicio relativa /api/empresas (handler en el mismo sitio; evita choque con Empresas.aspx).
  AB = menú visible; CJ = menú oculto; API keys distintas.
*/
MERGE dbo.ConfiguracionCanal AS t
USING (VALUES
    ('AB', N'/api/empresas', N'key-canal-ab-demo', CAST(1 AS BIT), N'Detalle.aspx', 5),
    ('CJ', N'/api/empresas', N'key-canal-cj-demo', CAST(0 AS BIT), N'Detalle.aspx', 5)
) AS s (Canal, UrlServicio, ApiKey, MostrarMenu, PaginaDestino, TimeoutSegundos)
ON t.Canal = s.Canal
WHEN MATCHED THEN
    UPDATE SET
        UrlServicio = s.UrlServicio,
        ApiKey = s.ApiKey,
        MostrarMenu = s.MostrarMenu,
        PaginaDestino = s.PaginaDestino,
        TimeoutSegundos = s.TimeoutSegundos,
        Activo = 1
WHEN NOT MATCHED THEN
    INSERT (Canal, UrlServicio, ApiKey, MostrarMenu, PaginaDestino, TimeoutSegundos, Activo)
    VALUES (s.Canal, s.UrlServicio, s.ApiKey, s.MostrarMenu, s.PaginaDestino, s.TimeoutSegundos, 1);
GO

EXEC dbo.usp_ConfiguracionCanal_Obtener @Canal = 'AB';
EXEC dbo.usp_ConfiguracionCanal_Obtener @Canal = 'CJ';
GO
