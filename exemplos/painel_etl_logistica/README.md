# 📦 Yvira Logistics WMS - Pipeline ETL & Dashboard Analytics

Painel analítico e operacional de Centro de Distribuição (Supply Chain & WMS) com **Pipeline ETL em tempo real** rodando nativamente na linguagem **Yvira**.

---

## 🚀 Como Executar

Dê um duplo clique no arquivo:
```cmd
iniciar.bat
```
Ou via terminal:
```powershell
..\..\dist\bin\yvira.exe run main.yv
```

---

## 📊 Indicadores e Métricas do Dashboard

1. **Separação (Picking):**
   - Total de unidades separadas vs. Meta do turno.
2. **Conferência Cega:**
   - Unidades conferidas e taxa de Acuracidade WMS em tempo real (99.1% ~ 99.8%).
3. **Movimentação Interna:**
   - Balanço entre **Movimentação Vertical** (Empilhadeiras / Racks aéreos) e **Translação Horizontal** (Transpaleteiras elétricas em ruas de picking).
4. **Faturamento & Vendas Logísticas:**
   - Faturamento do dia atualizado a cada ciclo do pipeline com contagem de pedidos faturados.
5. **Separação por Faixa Horária (Lotes):**
   - Gráfico dinâmico com colunas horárias (08:00 às 17:00), mostrando a evolução da produtividade do CD.
6. **Status por Zona de Armazenagem:**
   - Monitoramento das Zonas A, B, C, D e E com taxa de ocupação, status operacional (Crítico, Atenção, Normal, Livre) e quantidade de operadores alocados.
7. **Feed de Ingestão ETL ao Vivo:**
   - Registro de transações, separações, conferências e expedição em streaming contínuo.

---

## ⚙️ Arquitetura do Pipeline ETL Yvira

- **Extract & Transform (`main.yv`):** A função `executarEtlCiclo()` processa deltas de picking, reajusta médias ponderadas, recalcula taxas de ocupação e alimenta a fila de eventos.
- **Agendamento Contínuo:** Utiliza a diretiva nativa `@time.executarEtlCiclo(1.5)` para executar a cada 1.5 segundos em background.
- **Load / Streaming:** Envia o pacote estruturado de métricas diretamente para a visualização via `app.emitir("receber_dados_etl", pacoteDashboard)`.
