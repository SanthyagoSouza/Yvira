# 24 - Automações Nativas com `@time` e `@stop`

O Yvira agora suporta agendamentos nativos e automações reativas de funções sem depender de cron externo, arquivos separados ou sintaxes verbosas de bibliotecas de terceiros.

---

## 1. Como Funciona a Filosofia

Em vez de transformar a função em um cron fixo, você declara funções normalmente e as transforma em tarefas agendadas em **qualquer lugar do seu código** usando:

```yvira
@time.nome_da_funcao(intervalo_em_segundos, ...argumentos_opcionais);
```

E para interromper a automação quando uma condição for atingida:

```yvira
@stop.nome_da_funcao();
```

---

## 2. Exemplo Básico: Contador com Parada Automática

```yvira
let contador = 0;

fn tick() {
    contador = contador + 1;
    print("[TICK " + contador + "] Executado automaticamente!");
    
    # Quando atingir 5 execuções, para o agendamento desta função
    if (contador >= 5) {
        print("Finalizando tarefa agendada...");
        @stop.tick();
    }
}

# Inicia a automação: roda 'tick' a cada 2 segundos
@time.tick(2);

print("Fluxo principal livre. O Yvira mantém o scheduler ativo em background!");
```

---

## 3. Exemplo Avançado: Automação com Argumentos e Múltiplos Monitores

Você pode passar parâmetros para a função agendada e rodar múltiplos timers concorrentes:

```yvira
fn alertar_cotacao(moeda, alvo) {
    print("[ALERTA] Checando " + moeda + " para preco alvo: " + alvo);
}

fn monitor_saude() {
    print("[HEALTH] Servidor operacional.");
}

# Dispara alerta de BTC a cada 1 segundo com parâmetros
@time.alertar_cotacao(1, "BTC", 95000);

# Dispara alerta de ETH a cada 3 segundos com parâmetros
@time.alertar_cotacao(3, "ETH", 3200);

# Monitor de saúde do sistema a cada 5 segundos
@time.monitor_saude(5);
```

---

## 4. Integração no Runtime

- **Não bloqueante:** O agendamento é gerenciado pelo [`Scheduler.cs`](file:///C:/Users/santh/OneDrive/Área de Trabalho/ARQQUIVOS DRIVE/Beta OS/Yvira.Vm/Scheduler.cs) em threads concorrentes (`PeriodicTimer` / `ThreadPool`).
- **Loop de Eventos Inteligente:** O CLI do Yvira detecta automaticamente se há timers ativos e só finaliza o processo quando todas as tarefas `@time` forem paradas via `@stop` ou pelo sinal `Ctrl+C` no terminal.
- **Função continua comum:** Você pode chamar `tick()` manualmente a qualquer momento, além de ter as execuções automáticas.
