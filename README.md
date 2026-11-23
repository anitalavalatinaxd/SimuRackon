# SimuRackon

Simulador educativo de control de tráfico aéreo para Técnicas de Programación, Facultad de Ingeniería, UNAM. El escenario y sus reglas son ficticios.

## Ejecutarlo

1. Extrae el ZIP completo.
2. En Windows, abre `SimuRackon.sln` con Visual Studio 2022 actualizado y la carga de trabajo **Desarrollo de escritorio de .NET** con **.NET 8**.
3. Selecciona **SimuRackon** como proyecto de inicio, si Visual Studio lo solicita, y presiona **F5**.

Desde una terminal en Windows con el SDK de .NET 8 también puedes usar `dotnet run --project SimuRackon.Desktop`. El proyecto usa las bibliotecas de .NET y Windows Forms.

Para abrir la versión compilada en Windows de 64 bits, ejecuta `Aplicacion/SimuRackon.exe` con el entorno de ejecución **.NET Desktop Runtime 8** instalado. Conserva juntos los archivos de esa carpeta.

## Una ronda de la simulación

- Cada ronda dura hasta **3 minutos**. El reloj comienza en **03:00** cuando aparece el tablero. Selecciona en el mapa o en las seis tarjetas el vuelo que quieres atender.
- Hay tres llegadas en aproximación y tres salidas, una en cada pista. Autorizar un despegue libera la pista para un aterrizaje. Cada vuelo muestra su distancia a A, B y C.
- Para aterrizar, selecciona una pista y una terminal compatibles. A conecta con T1, B con T1 o T2 y C con T2 o T3. El vuelo que aterriza ocupa su terminal y la pista queda disponible.
- Para despegar, selecciona el vuelo en pista y presiona **Autorizar despegue**. El clima recorre Soleado, Nublado y Lluvioso cada 45 segundos. El botón **Cambiar** también permite avanzar al siguiente estado.
- La ronda empieza con 100 puntos. Una autorización correcta suma 20, un aterrizaje en pista o terminal ocupada resta 10 y un intento de despegar bajo lluvia resta 20. La bitácora explica cada resultado.
- Cuando el reloj llega a **00:00** o completas los **seis vuelos**, aparece una ventana con el motivo del cierre, la puntuación y los vuelos completados. **Iniciar otra simulación** genera seis vuelos nuevos y restablece el tiempo y los 100 puntos iniciales. **Ver bitácora** permite revisar las decisiones de la ronda finalizada. El resultado se guarda en `%LOCALAPPDATA%\SimuRackon\sesiones.csv`.

## Código

| Archivo | Función |
| --- | --- |
| `Simulacion.cs` | Generación de vuelos, cambios de clima, validación y puntuación. |
| `Vuelo.cs` | Identificador, estado, distancias y transiciones de cada vuelo. |
| `Aeropuerto.cs` | Pistas, terminales, conexiones y distancias. |
| `ResultadosForm.cs` | Ventana de puntuación final y botón para iniciar otra simulación. |
| `TorreForm.cs` | Interfaz de Windows Forms y acciones del usuario. |
| `MapaAeropuerto.cs` | Dibujo del aeropuerto y selección de vuelos en el mapa. |
| `RegistroSesiones.cs` | Guardado local de los resultados de cada ronda. |
