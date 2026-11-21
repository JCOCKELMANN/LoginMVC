
-- ============================================
-- 5. INSERTAR USUARIO ADMINISTRADOR
-- ============================================
USE DB_LoginMVC;
GO
INSERT INTO Usuarios (
    Nombre,
    Apellido,
    Username,
    PasswordHash,
    IdRol,
    IdPuesto,
    NivelAcceso,
    Activo
)
VALUES (
    'Administrador',
    'Sistema',
    'admin',
    '$2a$11$FQQKXYG0WLsi6Av6YEUMR.Qhuwv/1XaEA9Wd2AD/cyS.k3ruSfQG6',  
    --Admin12/
    (SELECT IdRol FROM Roles
     WHERE NombreRol = 'Administrador'),
    NULL,
    10,
    1
);
GO
