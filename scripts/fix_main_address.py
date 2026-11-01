import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\exemplos\click_battle_game\main.yv"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

target = 'let enderecoServidor = "ws://192.168.0.35:7777";'
replacement = """# Tenta conectar via localhost primeiro se rodar na mesma máquina, ou IP via argumento
let enderecoServidor = "ws://localhost:7777";
if (args != null && len(args) > 0 && args[0] != "") {
    enderecoServidor = args[0];
}"""

if target in c:
    c = c.replace(target, replacement)
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("MAIN_ADDRESS_FIXED")
else:
    print("TARGET_NOT_FOUND")
