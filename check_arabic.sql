SELECT nom, "NomArabe", encode("NomArabe"::bytea, 'hex') as hex_value FROM hammam;
