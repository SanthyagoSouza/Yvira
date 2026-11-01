# 13 - Sistema de Módulos e Importação de Arquivos

A linguagem **Yvira** agora possui um sistema nativo e simples de modularização, permitindo organizar código em múltiplos arquivos .yv.

---

## 1. Importação Direta

Importa todos os símbolos (funções, variáveis e constantes) de um arquivo para o escopo atual:

\\\yvira
import "modulo_matematica.yv"
# Ou sem a extensão .yv:
import "modulo_matematica"

let resultado = somar(10, 20)
print("Resultado:", resultado)
\\\

---

## 2. Importação com Namespace (s)

Permite isolar as funções e dados de um módulo dentro de um objeto/mapa, prevenindo colisão de nomes:

\\\yvira
import "modulo_matematica" as mat

print("Versão:", mat.versao)
let total = mat.multiplicar(6, 7)
print("Total:", total)
\\\

---

## 3. Resolução de Caminhos e Proteção de Ciclos

- **Caminhos relativos:** A busca dos arquivos importados é resolvida em relação ao diretório do arquivo atual que está executando o import.
- **Cache de Módulos:** Arquivos importados são analisados uma única vez. Se dois arquivos importam o mesmo módulo, o resultado em cache é reaproveitado.
- **Proteção contra Importação Circular:** Ciclos de importação (como A importando B e B importando A) não causam loop infinito nem travamento.
