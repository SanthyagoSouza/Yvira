using System;
using System.Collections.Generic;

namespace Yvira.Vm;

public static class SecurityModule
{
    public static YviraMap CreateModule(Interpreter interpreter)
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // security.sandbox({ fs: "readonly", net: ["api.banco.com"], allowProcess: false })
        methods["sandbox"] = new HostFunction("security.sandbox", (interp, args) =>
        {
            if (args.Count == 0 || args[0] is not YviraMap config)
            {
                throw new Exception("security.sandbox requer um mapa de configurações.");
            }

            var policy = interp.SecurityPolicy;
            policy.Enabled = true;

            // 1. Configuração de Sistema de Arquivos (FS)
            var fsVal = config.Get("fs");
            if (fsVal is string fsStr)
            {
                if (fsStr.Equals("none", StringComparison.OrdinalIgnoreCase) || fsStr.Equals("block", StringComparison.OrdinalIgnoreCase))
                {
                    policy.AllowFsRead = false;
                    policy.AllowFsWrite = false;
                }
                else if (fsStr.Equals("readonly", StringComparison.OrdinalIgnoreCase) || fsStr.Equals("read", StringComparison.OrdinalIgnoreCase))
                {
                    policy.AllowFsRead = true;
                    policy.AllowFsWrite = false;
                }
                else if (fsStr.Equals("full", StringComparison.OrdinalIgnoreCase) || fsStr.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    policy.AllowFsRead = true;
                    policy.AllowFsWrite = true;
                }
            }
            else if (fsVal is YviraMap fsMap)
            {
                var rList = fsMap.Get("read") as YviraList;
                if (rList != null)
                {
                    policy.AllowFsRead = false;
                    foreach (var p in rList.Items)
                    {
                        if (p != null) policy.AllowedReadPaths.Add(p.ToString()!);
                    }
                }

                var wList = fsMap.Get("write") as YviraList;
                if (wList != null)
                {
                    policy.AllowFsWrite = false;
                    foreach (var p in wList.Items)
                    {
                        if (p != null) policy.AllowedWritePaths.Add(p.ToString()!);
                    }
                }
            }

            // 2. Configuração de Rede (Net)
            var netVal = config.Get("net");
            if (netVal is bool bNet)
            {
                policy.AllowNet = bNet;
            }
            else if (netVal is YviraList netList)
            {
                policy.AllowNet = false;
                foreach (var h in netList.Items)
                {
                    if (h != null) policy.AllowedHosts.Add(h.ToString()!);
                }
            }

            // 3. Processos externos
            var procVal = config.Get("allowProcess") ?? config.Get("process");
            if (procVal is bool bProc)
            {
                policy.AllowProcessExecution = bProc;
            }

            // 4. Variáveis de ambiente
            var envVal = config.Get("allowEnv") ?? config.Get("env");
            if (envVal is bool bEnv)
            {
                policy.AllowEnvironmentRead = bEnv;
            }

            // 5. Anti-SQL Injection Taint Tracking
            var sqlVal = config.Get("antiSqlInjection") ?? config.Get("taintTracking");
            if (sqlVal is bool bSql)
            {
                policy.EnforceAntiSqlInjection = bSql;
            }

            return true;
        });

        // security.taint(valor, [origem]) -> TaintedString
        methods["taint"] = new HostFunction("security.taint", (interp, args) =>
        {
            if (args.Count == 0) return null;
            string val = args[0]?.ToString() ?? "";
            string src = args.Count > 1 ? args[1]?.ToString() ?? "manual" : "manual";
            return new TaintedString(val, src);
        });

        // security.isTainted(valor) -> bool
        methods["isTainted"] = new HostFunction("security.isTainted", (interp, args) =>
        {
            if (args.Count == 0) return false;
            return TaintedString.IsTainted(args[0]);
        });

        // security.sanitize(valor) -> string (Limpa o taint após validação/sanitização aprovada)
        methods["sanitize"] = new HostFunction("security.sanitize", (interp, args) =>
        {
            if (args.Count == 0) return "";
            return TaintedString.GetRawString(args[0]);
        });

        return new YviraMap(methods);
    }
}
