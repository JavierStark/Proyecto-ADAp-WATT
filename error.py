from lxml import etree

ruta = "C:/Users/ivanp/Desktop/EntityRelationship.xmi"  # Cambia si tu archivo tiene otro nombre

try:
    etree.parse(ruta)
    print("✅ El archivo XML está bien formado.")
except etree.XMLSyntaxError as e:
    print("❌ Error encontrado:")
    print(f"Línea: {e.lineno}, Columna: {e.position[1]}")
    print(f"Descripción: {e.msg}")

    # Mostrar unas líneas alrededor del error
    with open(ruta, "r", encoding="utf-8", errors="ignore") as f:
        lineas = f.readlines()
        ini = max(e.lineno - 3, 0)
        fin = min(e.lineno + 2, len(lineas))
        print("\n--- Fragmento del archivo ---")
        for i in range(ini, fin):
            marcador = "👉" if i + 1 == e.lineno else "  "
            print(f"{marcador} Línea {i+1}: {lineas[i].rstrip()}")
