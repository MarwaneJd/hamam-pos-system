UPDATE employe SET "Icone" = 'User1' WHERE username = 'vero';
UPDATE employe SET "Icone" = 'User2' WHERE username != 'vero' AND username != 'admin';
SELECT username, "Icone" FROM employe;
