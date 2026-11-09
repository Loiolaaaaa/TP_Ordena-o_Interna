#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

class Transacao
{
    public int Id { get; set; }
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    public string Status { get; set; }

    public override string ToString()
        => $"ID:{Id:D4} | R${Valor:F2} | {Data:dd/MM/yyyy} | {Status}";
}

static class Ordenadores
{
    public static void Insercao(Transacao[] v)
    {
        for (int i = 1; i < v.Length; i++)
        {
            Transacao atual = v[i];
            int pos = i;

            while (pos > 0 && v[pos - 1].Valor > atual.Valor)
            {
                v[pos] = v[pos - 1];
                pos--;
            }
            v[pos] = atual;
        }
    }

    public static void Intercalacao(Transacao[] v)
    {
        if (v.Length < 2) return;
        Transacao[] temp = new Transacao[v.Length];
        Dividir(v, temp, 0, v.Length - 1);
    }

    static void Dividir(Transacao[] v, Transacao[] temp, int ini, int fim)
    {
        if (ini >= fim) return;

        int centro = ini + (fim - ini) / 2;
        Dividir(v, temp, ini, centro);
        Dividir(v, temp, centro + 1, fim);
        Unir(v, temp, ini, centro, fim);
    }

    static void Unir(Transacao[] v, Transacao[] temp, int ini, int centro, int fim)
    {
        if (v[centro].Valor <= v[centro + 1].Valor) return;

        Array.Copy(v, ini, temp, ini, fim - ini + 1);

        int e = ini;
        int d = centro + 1;
        int destino = ini;

        while (e <= centro && d <= fim)
            v[destino++] = temp[e].Valor <= temp[d].Valor ? temp[e++] : temp[d++];

        while (e <= centro) v[destino++] = temp[e++];
        while (d <= fim) v[destino++] = temp[d++];
    }

    public static void Rapida(Transacao[] v)
    {
        if (v.Length < 2) return;
        Particionar(v, 0, v.Length - 1);
    }

    static void Particionar(Transacao[] v, int ini, int fim)
    {
        while (ini < fim)
        {
            decimal pivo = Mediana(v[ini].Valor, v[ini + (fim - ini) / 2].Valor, v[fim].Valor);
            int e = ini;
            int d = fim;

            while (e <= d)
            {
                while (v[e].Valor < pivo) e++;
                while (v[d].Valor > pivo) d--;

                if (e <= d)
                {
                    (v[e], v[d]) = (v[d], v[e]);
                    e++;
                    d--;
                }
            }

            if (d - ini < fim - e)
            {
                Particionar(v, ini, d);
                ini = e;
            }
            else
            {
                Particionar(v, e, fim);
                fim = d;
            }
        }
    }

    static decimal Mediana(decimal a, decimal b, decimal c)
    {
        if (a > b) (a, b) = (b, a);
        if (b > c) (b, c) = (c, b);
        if (a > b) (a, b) = (b, a);
        return b;
    }
}

class Programa
{
    const int LIMITE_INSERCAO = 100_000;
    const long BYTES_POR_TRANSACAO = 64 + 3 * 8;

    static readonly string[] Situacoes = { "Aprovada", "Pendente", "Recusada", "Estornada" };

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        CultureInfo pt = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = pt;
        CultureInfo.CurrentCulture = pt;

        int[] quantidades = { 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000 };

        Console.WriteLine("=== COMPARAÇÃO DE ORDENAÇÃO DE TRANSAÇÕES: Inserção x Intercalação x Rápida ===\n");

        Aquecer();

        var tabela = new List<string>
        {
            $"{"n",15} | {"Inserção (ms)",16} | {"Merge (ms)",14} | {"Quick (ms)",14}",
            new string('-', 68)
        };

        foreach (int n in quantidades)
        {
            Console.WriteLine($">>> Gerando {n:N0} transações ...");

            if (!CabeNaMemoria(n))
            {
                Console.WriteLine($"    Memória insuficiente para n = {n:N0}. Pulando.\n");
                tabela.Add(LinhaTabela(n, "sem memória", "sem memória", "sem memória"));
                continue;
            }

            Transacao[] dados = null;
            string tIns = "pulado (O(n²))";
            string tMer, tQui;
            Transacao[] ordenadoRapida;

            try
            {
                dados = Gerar(n, 42);

                if (n <= LIMITE_INSERCAO)
                    tIns = Executar(dados, Ordenadores.Insercao, out _);

                tMer = Executar(dados, Ordenadores.Intercalacao, out _);
                tQui = Executar(dados, Ordenadores.Rapida, out ordenadoRapida);
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine($"    Memória esgotada durante o teste com n = {n:N0}.\n");
                tabela.Add(LinhaTabela(n, "-", "sem memória", "sem memória"));
                dados = null;
                GC.Collect();
                continue;
            }

            tabela.Add(LinhaTabela(n, tIns, tMer, tQui));
            Console.WriteLine($"    Tempos (ms) -> Inserção: {tIns} | Merge: {tMer} | Quick: {tQui}");
            MostrarMaiores(ordenadoRapida);

            dados = null;
            ordenadoRapida = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine("\n=== RESUMO DOS TEMPOS ===");
        foreach (string linha in tabela)
            Console.WriteLine(linha);

        Console.WriteLine("\n(✓ = vetor conferido e ordenado corretamente)");
    }

    static string LinhaTabela(int n, string a, string b, string c)
        => $"{n,15:N0} | {a,16} | {b,14} | {c,14}";

    static bool CabeNaMemoria(int n)
    {
        long disponivel = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        return (long)n * BYTES_POR_TRANSACAO < disponivel * 0.8;
    }

    static void Aquecer()
    {
        Transacao[] amostra = Gerar(200, 1);
        Ordenadores.Insercao((Transacao[])amostra.Clone());
        Ordenadores.Intercalacao((Transacao[])amostra.Clone());
        Ordenadores.Rapida((Transacao[])amostra.Clone());
    }

    static string Executar(Transacao[] origem, Action<Transacao[]> algoritmo, out Transacao[] resultado)
    {
        resultado = (Transacao[])origem.Clone();

        Stopwatch cronometro = Stopwatch.StartNew();
        algoritmo(resultado);
        cronometro.Stop();

        bool correto = Ordenado(resultado);
        return $"{cronometro.Elapsed.TotalMilliseconds:N2} {(correto ? "✓" : "✗")}";
    }

    static Transacao[] Gerar(int n, int semente)
    {
        var aleatorio = new Random(semente);
        var vetor = new Transacao[n];
        DateTime referencia = new DateTime(2026, 1, 1);

        for (int i = 0; i < n; i++)
        {
            vetor[i] = new Transacao
            {
                Id = i + 1,
                Valor = aleatorio.Next(1, 100_000_000) / 100m,
                Data = referencia.AddDays(-aleatorio.Next(0, 365)),
                Status = Situacoes[aleatorio.Next(Situacoes.Length)]
            };
        }
        return vetor;
    }

    static bool Ordenado(Transacao[] v)
    {
        for (int i = 1; i < v.Length; i++)
            if (v[i - 1].Valor > v[i].Valor) return false;
        return true;
    }

    static void MostrarMaiores(Transacao[] ordenado)
    {
        int qtd = Math.Min(10, ordenado.Length);
        Console.WriteLine($"    Top {qtd} maiores transações:");

        for (int i = 0; i < qtd; i++)
            Console.WriteLine($"    {i + 1,2}. {ordenado[ordenado.Length - 1 - i]}");

        Console.WriteLine();
    }
}
