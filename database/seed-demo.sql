-- Charlie's Sneakers POS - datos de demostración para la interfaz de caja.
-- Idempotente: cada producto se inserta solo si su SKU todavía no existe,
-- por lo que puede ejecutarse varias veces sin duplicar ni modificar datos.
-- Uso: make db-seed   (contra la base aislada DB_NAME en LocalDB)
-- PV-AJ1-BRD-43 tiene stock 0 para probar el escenario V-02 (sin existencias).

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Products TABLE (
    Id        UNIQUEIDENTIFIER NOT NULL,
    Sku       NVARCHAR(64)     NOT NULL,
    Barcode   NVARCHAR(64)     NULL,
    Name      NVARCHAR(200)    NOT NULL,
    Size      NVARCHAR(30)     NOT NULL,
    Color     NVARCHAR(80)     NOT NULL,
    UnitPrice DECIMAL(18, 2)   NOT NULL,
    Stock     INT              NOT NULL
);

INSERT INTO @Products (Id, Sku, Barcode, Name, Size, Color, UnitPrice, Stock)
VALUES
    ('6f1d2c10-0001-4a00-9000-000000000001', 'PV-AJ1-CHI-42', '7441000000011', N'Air Jordan 1 Retro High OG', '42', 'Chicago Rojo/Blanco', 95000.00, 5),
    ('6f1d2c10-0001-4a00-9000-000000000002', 'PV-AJ1-CHI-43', '7441000000028', N'Air Jordan 1 Retro High OG', '43', 'Chicago Rojo/Blanco', 95000.00, 3),
    ('6f1d2c10-0001-4a00-9000-000000000003', 'PV-AJ1-BRD-43', '7441000000035', N'Air Jordan 1 Retro High OG', '43', 'Bred Negro/Rojo',     98000.00, 0),
    ('6f1d2c10-0001-4a00-9000-000000000004', 'PV-AF1-WHT-40', '7441000000042', N'Nike Air Force 1 ''07',      '40', 'Blanco',              65000.00, 8),
    ('6f1d2c10-0001-4a00-9000-000000000005', 'PV-AF1-WHT-41', '7441000000059', N'Nike Air Force 1 ''07',      '41', 'Blanco',              65000.00, 6),
    ('6f1d2c10-0001-4a00-9000-000000000006', 'PV-AF1-BLK-42', '7441000000066', N'Nike Air Force 1 ''07',      '42', 'Negro',               65000.00, 4),
    ('6f1d2c10-0001-4a00-9000-000000000007', 'PV-DNK-PND-39', '7441000000073', N'Nike Dunk Low Panda',        '39', 'Blanco/Negro',        72000.00, 2),
    ('6f1d2c10-0001-4a00-9000-000000000008', 'PV-SMB-GRN-41', '7441000000080', N'Adidas Samba OG',            '41', 'Blanco/Verde',        58000.00, 7),
    ('6f1d2c10-0001-4a00-9000-000000000009', 'PV-NB550-GRY-44','7441000000097', N'New Balance 550',            '44', 'Blanco/Gris',         69000.00, 1);

INSERT INTO dbo.ProductVariants (Id, Sku, Barcode, Name, Size, Color, UnitPrice, Stock)
SELECT p.Id, p.Sku, p.Barcode, p.Name, p.Size, p.Color, p.UnitPrice, p.Stock
FROM @Products AS p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ProductVariants AS existing WHERE existing.Sku = p.Sku
);

DECLARE @Inserted INT = @@ROWCOUNT;

COMMIT TRANSACTION;

PRINT CONCAT('Productos de demostracion insertados: ', @Inserted, ' (los SKU existentes no se modifican).');
