namespace SimuRackon.Core;

public enum CondicionClimatica { Soleado, Nublado, Lluvioso }

public enum FinDeRonda { VuelosCompletados, TiempoAgotado }

public sealed record ResultadoAutorizacion(string Titulo, string Explicacion, int CambioPuntos)
{
    public bool Aceptada => CambioPuntos > 0;
}

public sealed class Simulacion
{
    public const int CantidadVuelos = 6;
    public static readonly TimeSpan Duracion = TimeSpan.FromMinutes(3);
    private readonly TimeProvider reloj;
    private readonly long inicio;
    private TimeSpan? restanteAlCompletar;

    private static readonly string[] Prefijos = ["AMX", "VOI", "VIV", "MXA", "LNE", "RKN"];

    public IReadOnlyList<Vuelo> Vuelos { get; }
    public CondicionClimatica Clima { get; private set; } = CondicionClimatica.Soleado;
    public int Puntos { get; private set; } = 100;
    public int Aciertos { get; private set; }
    public int Errores { get; private set; }
    public int Completados => Vuelos.Count(vuelo => vuelo.Completado);
    public TimeSpan TiempoRestante
    {
        get
        {
            if (restanteAlCompletar is TimeSpan restanteFinal) return restanteFinal;
            TimeSpan restante = Duracion - reloj.GetElapsedTime(inicio);
            return restante > TimeSpan.Zero ? restante : TimeSpan.Zero;
        }
    }

    public FinDeRonda? MotivoDeFin
    {
        get
        {
            if (restanteAlCompletar.HasValue) return FinDeRonda.VuelosCompletados;
            if (TiempoRestante == TimeSpan.Zero) return FinDeRonda.TiempoAgotado;
            return null;
        }
    }

    public bool Finalizada => MotivoDeFin.HasValue;
    public int? Porcentaje => Aciertos + Errores == 0
        ? null
        : (int)Math.Round(100.0 * Aciertos / (Aciertos + Errores), MidpointRounding.AwayFromZero);

    public string Visibilidad => Clima switch
    {
        CondicionClimatica.Soleado => "Buena",
        CondicionClimatica.Nublado => "Reducida",
        _ => "Regular"
    };

    public Simulacion(Random? aleatorio = null, TimeProvider? reloj = null)
    {
        aleatorio ??= Random.Shared;
        this.reloj = reloj ?? TimeProvider.System;
        var pistasSalida = Aeropuerto.Pistas.ToArray();
        aleatorio.Shuffle(pistasSalida);
        int primerPrefijo = aleatorio.Next(Prefijos.Length);

        var vuelos = new Vuelo[CantidadVuelos];
        for (int i = 0; i < vuelos.Length; i += 2)
        {
            int pareja = i / 2;
            Pista pista = pistasSalida[pareja];
            string salida = Prefijos[(primerPrefijo + i) % Prefijos.Length] + aleatorio.Next(120, 983);
            string llegada = Prefijos[(primerPrefijo + i + 1) % Prefijos.Length] + aleatorio.Next(120, 983);
            vuelos[i] = new Vuelo(salida, TipoOperacion.Salida, Aeropuerto.DistanciasDesdePista(pista), pista);

            var (x, y) = pareja switch
            {
                0 => (aleatorio.Next(-12, -3), aleatorio.Next(-12, 13)),
                1 => (aleatorio.Next(34, 44), aleatorio.Next(7, 25)),
                _ => (aleatorio.Next(-16, -5), aleatorio.Next(17, 29))
            };
            vuelos[i + 1] = new Vuelo(llegada, TipoOperacion.Llegada, Aeropuerto.DistanciasDesde(x, y));
        }

        Vuelos = Array.AsReadOnly(vuelos);
        inicio = this.reloj.GetTimestamp();
    }

    public void CambiarClima()
    {
        if (Finalizada) return;
        Clima = Clima switch
        {
            CondicionClimatica.Soleado => CondicionClimatica.Nublado,
            CondicionClimatica.Nublado => CondicionClimatica.Lluvioso,
            _ => CondicionClimatica.Soleado
        };
    }

    public Vuelo? Ocupante(Pista pista) =>
        Vuelos.FirstOrDefault(vuelo => !vuelo.Completado && vuelo.Pista == pista);

    public Vuelo? Ocupante(Terminal terminal) =>
        Vuelos.FirstOrDefault(vuelo => vuelo.Terminal == terminal);

    public ResultadoAutorizacion? Autorizar(Vuelo vuelo, Pista? pista = null, Terminal? terminal = null)
    {
        // Evaluar la autorización con el tiempo que quedaba al recibir la decisión.
        TimeSpan restante = TiempoRestante;
        if (restante == TimeSpan.Zero || vuelo.Completado || !Vuelos.Contains(vuelo)) return null;

        var resultado = vuelo.Operacion == TipoOperacion.Salida
            ? EvaluarDespegue(vuelo)
            : EvaluarAterrizaje(vuelo, pista, terminal);
        if (resultado is null) return null;

        Puntos += resultado.CambioPuntos;
        if (resultado.Aceptada)
        {
            Aciertos++;
            if (Completados == Vuelos.Count) restanteAlCompletar = restante;
        }
        else
        {
            Errores++;
        }
        return resultado;
    }

    private ResultadoAutorizacion EvaluarDespegue(Vuelo vuelo)
    {
        if (Clima == CondicionClimatica.Lluvioso)
        {
            return new("Despegue rechazado",
                $"La lluvia suspende los despegues. {vuelo.Identificador} permanece en la pista {vuelo.Pista}.", -20);
        }
        vuelo.Despegar();
        return new("Despegue autorizado",
            $"{vuelo.Identificador} despegó y liberó la pista {vuelo.Pista}.", 20);
    }

    private ResultadoAutorizacion? EvaluarAterrizaje(Vuelo vuelo, Pista? pista, Terminal? terminal)
    {
        if (pista is not Pista pistaLlegada || terminal is not Terminal terminalLlegada ||
            !Aeropuerto.Pistas.Contains(pistaLlegada) || !Aeropuerto.TerminalesDe(pistaLlegada).Contains(terminalLlegada))
        {
            return null;
        }

        if (Ocupante(pistaLlegada) is Vuelo vueloEnPista)
        {
            return new("Aterrizaje rechazado",
                $"La pista {pistaLlegada} está ocupada por {vueloEnPista.Identificador}. {vuelo.Identificador} sigue esperando pista.", -10);
        }

        if (Ocupante(terminalLlegada) is Vuelo vueloEnTerminal)
        {
            return new("Aterrizaje rechazado",
                $"La terminal {terminalLlegada} está ocupada por {vueloEnTerminal.Identificador}. {vuelo.Identificador} sigue esperando pista.", -10);
        }

        vuelo.Aterrizar(pistaLlegada, terminalLlegada);
        return new("Aterrizaje autorizado",
            $"{vuelo.Identificador} aterrizó por la pista {pistaLlegada} y ocupa la terminal {terminalLlegada}.", 20);
    }

}
