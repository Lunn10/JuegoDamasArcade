using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Reflection;
public class Movimiento {
    public int fila { get; set; }
    public int columna { get; set; }
    public List<Tuple<int, int>> capturas { get; set; }
    public List<Tuple<int, int>> movimientosIntermedios { get; set; }
    public (int fila, int columna) ? posicionAnteriorDama { get; set; }

    public Movimiento() {
        capturas = new List<Tuple<int, int>>();
        movimientosIntermedios = new List<Tuple<int, int>>();
        posicionAnteriorDama = null;
    }

    public Movimiento(int fila, int columna) {
        this.fila = fila;
        this.columna = columna;
        capturas = new List<Tuple<int, int>>();
        movimientosIntermedios = new List<Tuple<int, int>>();
        posicionAnteriorDama = null;
    }

    public void agregarCapturas(int fila, int columna) {
        Debug.Log("Agregando captura: " + fila + ", " + columna);
        capturas.Add(new Tuple<int, int>(fila, columna));
    }
}

public class Ficha : MonoBehaviour {
    private int posicionColumna;
    private int posicionFila;
    private bool esBlanca = false;
    private bool esDama = false;
    private Renderer fichaRenderer;
    private Color colorOriginal;
    private GenerarTablero tableroComponente;
    private List<Movimiento> movimientos = new List<Movimiento>();
    private Vector3 posicionOriginal; // Posición original de la ficha
    private bool estaSiendoArrastrada = false; // Bandera para saber si la ficha está siendo arrastrada
    public ParticleSystem particulas;

    void Start() {
        fichaRenderer = GetComponent<Renderer>();
        colorOriginal = fichaRenderer.material.color;
        tableroComponente = GetComponentInParent<GenerarTablero>();
    }

    void OnMouseEnter() {
        limpiarMovimientosPosibles();

        if ((esBlanca && tableroComponente.getTurno() == "BLANCAS") || (!esBlanca && tableroComponente.getTurno() == "NEGRAS")) {
            fichaRenderer.material.color = Color.green;
            calcularMovimientosPosibles(posicionFila, posicionColumna);
            mostrarMovimientosPosibles();
        }
    }

    void OnMouseExit() {
        if (!estaSiendoArrastrada) {
            fichaRenderer.material.color = colorOriginal;

            limpiarMovimientosPosibles();
        }
    }

    void OnMouseDown() {
        if ((esBlanca && tableroComponente.getTurno() == "BLANCAS") || (!esBlanca && tableroComponente.getTurno() == "NEGRAS")) {
            posicionOriginal = transform.position;
            estaSiendoArrastrada = true;
            fichaRenderer.material.color = Color.yellow;
        }
    }

    void OnMouseDrag() {
        if (estaSiendoArrastrada) {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
            transform.position = new Vector3(mousePosition.x, transform.position.y, mousePosition.z);
        }
    }

    void OnMouseUp() {
        if (estaSiendoArrastrada) {
            gestionarMovimiento();

            // Restauramos el estado de la ficha
            estaSiendoArrastrada = false;
            fichaRenderer.material.color = colorOriginal;
        }
    }

    public void fichaBlanca() {
        esBlanca = true;
    }

    void coronarFicha() {
        esDama = true;
    }

    public bool fichaEsDama() {
        return esDama;
    }

    public bool esFichaBlanca() {
        return esBlanca;
    }

    public void setPosicion(int fila, int columna) {
        posicionFila = fila;
        posicionColumna = columna;
    }

    void gestionarMovimiento() {
        Vector3 posicionFinal = transform.position;
        bool movimientoValido = false;
        Movimiento movimientoSeleccionado = null;

        foreach (var movimiento in movimientos) {
            GameObject casillaDestino = tableroComponente.getTablero()[movimiento.fila, movimiento.columna];
            Vector3 casillaPosicion = casillaDestino.transform.position;
            float distancia = Vector3.Distance(posicionFinal, casillaPosicion);

            if (distancia < 0.5f) {
                movimientoSeleccionado = movimiento;
                movimientoValido = true;
                break;
            }
        }

        if (movimientoValido) {
            tableroComponente.getTablero()[posicionFila, posicionColumna].GetComponent<Casilla>().liberarCasilla();
            transform.position = tableroComponente.getTablero()[movimientoSeleccionado.fila, movimientoSeleccionado.columna].transform.position + new Vector3(0, 0.3f, 0);
            posicionFila = movimientoSeleccionado.fila;
            posicionColumna = movimientoSeleccionado.columna;
            limpiarMovimientosPosibles();

            tableroComponente.getTablero()[movimientoSeleccionado.fila, movimientoSeleccionado.columna].GetComponent<Casilla>().setFicha(this);

//            debuggearCapturas(movimientoSeleccionado);

            if(movimientoSeleccionado.capturas.Count > 0) {
                foreach (var captura in movimientoSeleccionado.capturas) {
                    destruirFicha(captura.Item1, captura.Item2);
                }
            }

            if (esBlanca && posicionFila == 9) {
                coronarFicha();
            } else if (!esBlanca && posicionFila == 0) {
                coronarFicha();
            }

            tableroComponente.cambiarTurno();
        } else {
            transform.position = posicionOriginal;
        }
    }

    void calcularMovimientosPosibles(int posicionFila, int posicionColumna) {
        GameObject[,] tablero = tableroComponente.getTablero();

        if (esDama) {
            calcularMovimientosPosiblesDama(tablero, (posicionFila, posicionColumna), -1, -1);
            calcularMovimientosPosiblesDama(tablero, (posicionFila, posicionColumna), -1, 1);
            calcularMovimientosPosiblesDama(tablero, (posicionFila, posicionColumna), 1, -1);
            calcularMovimientosPosiblesDama(tablero, (posicionFila, posicionColumna), 1, 1);
        } else {
            calcularMovimientosPosiblesFicha(tablero, posicionFila, posicionColumna);
        }

        filtrarMovimientosPosibles();
    }

    void filtrarMovimientosPosibles() {
        List<Movimiento> movimientosFiltrados = new List<Movimiento>();
        int cantidadCapturas = 0;

        foreach (var movimiento in movimientos) {
            if(movimiento.capturas.Count > cantidadCapturas) {
                cantidadCapturas = movimiento.capturas.Count;
                movimientosFiltrados.Clear();
                movimientosFiltrados.Add(movimiento);
            } else if(movimiento.capturas.Count == cantidadCapturas) {
                movimientosFiltrados.Add(movimiento);
            }
        }

        if(movimientosFiltrados.Count > 0) {
            movimientos = movimientosFiltrados;
        }
    }

    void calcularMovimientosPosiblesDama(
        GameObject[,] tablero, 
        (int fila, int columna) origen, 
        int aumentoFila, 
        int aumentoColumna, 
        Movimiento movimientoAnterior = null, 
        bool movimientoDamaLuegoDeComer = false,
        bool movimientoEnLaMismaDireccion = false        
    ) {
        if(origen.fila + aumentoFila < 0 || origen.fila + aumentoFila >= 10 || origen.columna + aumentoColumna < 0 || origen.columna + aumentoColumna >= 10) {
            return;
        }

        if(casillaOcupada(origen.fila + aumentoFila, origen.columna + aumentoColumna) && !fichaYaComida (movimientoAnterior, origen.fila + aumentoFila, origen.columna + aumentoColumna)) {
            comprobarComerFichaDama(
                tablero, 
                origen, 
                (origen.fila + aumentoFila, origen.columna + aumentoColumna), 
                (origen.fila + aumentoFila * 2, origen.columna + aumentoColumna * 2),
                aumentoFila,
                aumentoColumna,
                movimientoAnterior
            );
        } else {
            Movimiento movimiento = new Movimiento(origen.fila + aumentoFila, origen.columna + aumentoColumna);

            if(movimientoDamaLuegoDeComer) {
                foreach (Tuple<int, int> captura in movimientoAnterior.capturas) {
                    movimiento.capturas.Add(captura);
                }

                if (movimientoAnterior.posicionAnteriorDama.HasValue) {
                    movimiento.movimientosIntermedios.Add(new Tuple<int, int>(movimientoAnterior.posicionAnteriorDama.Value.fila, movimientoAnterior.posicionAnteriorDama.Value.columna));
                }
            }

            if(!movimientoDamaLuegoDeComer) {
                movimientos.Add(movimiento);
            } else if(movimientoDamaLuegoDeComer && movimientoEnLaMismaDireccion) {
                movimientos.Add(movimiento);
            }

            calcularMovimientosPosiblesDama(tablero, (origen.fila + aumentoFila, origen.columna + aumentoColumna), aumentoFila, aumentoColumna, movimiento, movimientoDamaLuegoDeComer, movimientoEnLaMismaDireccion);

            if(movimientoDamaLuegoDeComer && movimientoEnLaMismaDireccion) {
                calcularMovimientosPosiblesDama(tablero, (origen.fila + aumentoFila, origen.columna + aumentoColumna), -aumentoFila, aumentoColumna, movimiento, movimientoDamaLuegoDeComer, false);
                calcularMovimientosPosiblesDama(tablero, (origen.fila + aumentoFila, origen.columna + aumentoColumna), aumentoFila, -aumentoColumna, movimiento, movimientoDamaLuegoDeComer, false);
            }
        }
    }

    void destruirFicha(int fila, int columna) {
        GameObject casilla = tableroComponente.getTablero()[fila, columna];
        Casilla casillaComponente = casilla.GetComponent<Casilla>();

        Ficha fichaCapturada = casillaComponente.getFicha();
        casillaComponente.liberarCasilla();

        if (fichaCapturada != null) {
            Destroy(fichaCapturada.gameObject);
            
            ParticleSystem particulasInstanciadas = Instantiate(particulas, transform.position, Quaternion.identity);
            particulasInstanciadas.GetComponent<ParticleSystemRenderer>().material.color = fichaRenderer.material.color;
            Destroy(particulasInstanciadas.gameObject, particulasInstanciadas.main.duration);
        }
    }

    void comprobarComerFichaDama (
        GameObject[,] tablero, 
        (int fila, int columna) origen, 
        (int fila, int columna) ocupada, 
        (int fila, int columna) libre,
        int aumentoFila, 
        int aumentoColumna, 
        Movimiento movimientoAnterior = null
    ) {
        if(ocupada.fila < 0 || ocupada.fila >= 10 || ocupada.columna < 0 || ocupada.columna >= 10) {
            return;
        }

        if(libre.fila < 0 || libre.fila >= 10 || libre.columna < 0 || libre.columna >= 10) {
            return;
        }
        
        bool esFichaBlanca = tablero[ocupada.fila, ocupada.columna].GetComponent<Casilla>().esFichaBlanca();

        if(esBlanca != esFichaBlanca && !fichaYaComida(movimientoAnterior, ocupada.fila, ocupada.columna)) {
            if (casillaOcupada(ocupada.fila, ocupada.columna) && !casillaOcupada(libre.fila, libre.columna)) {  
                Movimiento movimiento = new Movimiento(libre.fila, libre.columna);

                movimiento.capturas.Add(new Tuple<int, int>(ocupada.fila, ocupada.columna));

                if(movimientoAnterior != null) {
                    foreach (var captura in movimientoAnterior.capturas) {
                        movimiento.capturas.Add(captura);
                    }

                    if (movimientoAnterior.posicionAnteriorDama.HasValue) {
                        movimiento.movimientosIntermedios.Add(new Tuple<int, int>(
                            movimientoAnterior.posicionAnteriorDama.Value.fila, 
                            movimientoAnterior.posicionAnteriorDama.Value.columna
                        ));
                    }

                    if (movimientos.Contains(movimientoAnterior)) {
                        movimientos.Remove(movimientoAnterior);
                    }
                }

                movimiento.posicionAnteriorDama = (libre.fila, libre.columna);

                movimientos.Add(movimiento);

                calcularMovimientosPosiblesDama(tablero, (libre.fila, libre.columna), aumentoFila, aumentoColumna, movimiento, true, true);
                calcularMovimientosPosiblesDama(tablero, (libre.fila, libre.columna), -aumentoFila, aumentoColumna, movimiento, true);
                calcularMovimientosPosiblesDama(tablero, (libre.fila, libre.columna), aumentoFila, -aumentoColumna, movimiento, true);
            }
        }
    }

    bool fichaYaComida(Movimiento movimiento, int fila, int columna) {
        if(movimiento == null) {
            return false;
        }

        foreach (var captura in movimiento.capturas) {
            if(captura.Item1 == fila && captura.Item2 == columna) {
                return true;
            }
        }

        return false;
    }

    void calcularMovimientosPosiblesFicha(GameObject[,] tablero, int posicionFila, int posicionColumna) {
        int filaSiguiente = posicionFila + 1;
        int columnaAnterior = posicionColumna - 1;
        int columnaSiguiente = posicionColumna + 1;

        if(!esBlanca) {
            filaSiguiente = posicionFila - 1;
        }

        if (filaSiguiente > 0 && filaSiguiente < 10) {
            if (columnaAnterior >= 0) {
                bool estaOcupada = casillaOcupada(filaSiguiente, columnaAnterior);

                if (!estaOcupada) {
                    movimientos.Add(new Movimiento(filaSiguiente, columnaAnterior));
                } else {
                    comprobarComerFicha(tablero, (posicionFila, posicionColumna), (filaSiguiente, columnaAnterior), (esBlanca ? filaSiguiente + 1 : filaSiguiente - 1, columnaAnterior - 1) );
                }
            }

            if (columnaSiguiente < 10) {
                bool estaOcupada = casillaOcupada(filaSiguiente, columnaSiguiente);

                if (!estaOcupada) {
                    movimientos.Add(new Movimiento(filaSiguiente, columnaSiguiente));
                } else {
                    comprobarComerFicha(tablero, (posicionFila, posicionColumna), (filaSiguiente, columnaSiguiente),( esBlanca ? filaSiguiente + 1 : filaSiguiente - 1, columnaSiguiente + 1) );
                }
            }
        }
    }

    void comprobarComerFicha(GameObject[,] tablero, (int fila, int columna) posicionFicha, (int fila, int columna) ocupada, (int fila, int columna) libre, Movimiento movimientoAnterior = null) {
        if(ocupada.fila < 0 || ocupada.fila >= 10 || ocupada.columna < 0 || ocupada.columna >= 10) {
            return;
        }

        if(libre.fila < 0 || libre.fila >= 10 || libre.columna < 0 || libre.columna >= 10) {
            return;
        }
        
        bool esFichaBlanca = tablero[ocupada.fila, ocupada.columna].GetComponent<Casilla>().esFichaBlanca();

        if (casillaOcupada(ocupada.fila, ocupada.columna) && esBlanca != esFichaBlanca && !casillaOcupada(libre.fila, libre.columna)) {  
            Movimiento movimiento = new Movimiento(libre.fila, libre.columna);
            movimiento.capturas.Add(new Tuple<int, int>(ocupada.fila, ocupada.columna));
            movimiento.movimientosIntermedios.Add(new Tuple<int, int>(posicionFicha.fila, posicionFicha.columna));

            if(movimientoAnterior != null) {
                foreach (var captura in movimientoAnterior.capturas) {
                    movimiento.capturas.Add(captura);
                }

                foreach (var movimientoIntermedio in movimientoAnterior.movimientosIntermedios) {
                    movimiento.movimientosIntermedios.Add(movimientoIntermedio);
                }

                if (movimientos.Contains(movimientoAnterior)) {
                    movimientos.Remove(movimientoAnterior);
                }
            }

            movimientos.Add(movimiento);

            comprobarComerFicha(
                tablero, 
                libre, 
                (esBlanca ? libre.fila + 1 : libre.fila - 1, libre.columna - 1), 
                (esBlanca ? libre.fila + 2 : libre.fila - 2, libre.columna - 2), 
                movimiento
            );

            comprobarComerFicha(
                tablero, 
                libre, 
                (esBlanca ? libre.fila + 1 : libre.fila - 1, libre.columna + 1), 
                (esBlanca ? libre.fila + 2 : libre.fila - 2, libre.columna + 2), 
                movimiento
            );
       }
    }

    bool casillaOcupada(int fila, int columna) {
        GameObject[,] tablero = tableroComponente.getTablero();
        return tablero[fila, columna].GetComponent<Casilla>().getOcupada();
    }

    void mostrarMovimientosPosibles() {
        GameObject[,] tablero = tableroComponente.getTablero();
        foreach (var movimiento in movimientos) {
            tablero[movimiento.fila, movimiento.columna].GetComponent<Renderer>().material.color = Color.blue;

            foreach (var movimientoIntermedio in movimiento.movimientosIntermedios) {
                tablero[movimientoIntermedio.Item1, movimientoIntermedio.Item2].GetComponent<Renderer>().material.color = Color.green;
            }
        }
    }

    void debuggearCapturas(Movimiento movimiento) {
        foreach (var captura in movimiento.capturas) {
            Debug.Log("Captura: " + captura.Item1 + ", " + captura.Item2);
        }
    }

    void debuggearMovimientosIntermedios(Movimiento movimiento) {
        foreach (var movimientoIntermedio in movimiento.movimientosIntermedios) {
            Debug.Log("Movimiento intermedio: " + movimientoIntermedio.Item1 + ", " + movimientoIntermedio.Item2);
        }
    }

    void limpiarMovimientosPosibles() {
        // Limpiamos los movimientos posibles (restaurando color de las casillas)
        GameObject[,] tablero = tableroComponente.getTablero();
        foreach (var movimiento in movimientos) {
            tablero[movimiento.fila, movimiento.columna].GetComponent<Renderer>().material.color = Color.red;

            foreach (var movimientoIntermedio in movimiento.movimientosIntermedios) {
                tablero[movimientoIntermedio.Item1, movimientoIntermedio.Item2].GetComponent<Renderer>().material.color = Color.red;
            }
        }
        movimientos.Clear();
    }

    public void mostrarMovimiento(Movimiento movimiento) {
        string textoMovimiento = "";

        textoMovimiento += "Movimiento:\n";
        textoMovimiento += "Fila: " + movimiento.fila + "\n";
        textoMovimiento += "Columna: " + movimiento.columna + "\n";
        textoMovimiento += "Capturas:\n";

        foreach (Tuple<int, int> captura in movimiento.capturas) {
            textoMovimiento += "(" + captura.Item1 + ", " + captura.Item2 + ")\n";
        }

        textoMovimiento += "Movimientos intermedios:\n";

        foreach (Tuple<int, int> movimientoIntermedio in movimiento.movimientosIntermedios) {
            textoMovimiento += "(" + movimientoIntermedio.Item1 + ", " + movimientoIntermedio.Item2 + ")\n";
        }

        Debug.Log(textoMovimiento);
    }
}
