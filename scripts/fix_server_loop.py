import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\exemplos\click_battle_game\server.yv"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

target = """# Loop de vigilia do servidor
let rodando = true;
while (rodando) {
    # servidor ativo
}"""

replacement = """# Loop de vigilia saudavel do servidor
let rodando = true;
while (rodando) {
    dormir(100);
}"""

if target in c:
    c = c.replace(target, replacement)
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("SERVER_LOOP_FIXED")
else:
    print("TARGET_NOT_FOUND")
