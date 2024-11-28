using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public class Movimiento {
    public int fila { get; set; }
    public int columna { get; set; }
    public List<Tuple<int, int>> capturas { get; set; }
    public List<Tuple<int, int>> movimientosIntermedios { get; set; }

    public Movimiento(int fila, int columna) {
        this.fila = fila;
        this.columna = columna;
        capturas = new List<Tuple<int, int>>();
        movimientosIntermedios = new List<Tuple<int, int>>();
    }
}


public class Ficha : MonoBehaviour {
    private int posicionColumna;
    private int posicionFila;
    private bool esBlanca = false;
    private Renderer fichaRenderer;
    private Color colorOriginal;
    private GenerarTablero tableroComponente;
    private List<Movimiento> movimientos = new List<Movimiento>();
    private Vector3 posicionOriginal; // Posición original de la ficha
    private bool estaSiendoArrastrada = false; // Bandera para saber si la ficha está siendo arrastrada

    void Start() {
        fichaRenderer = GetComponent<Renderer>();
        colorOriginal = fichaRenderer.material.color;
        tableroComponente = GetComponentInParent<GenerarTablero>();
    }

    void OnMouseEnter() {        
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
            // Cuando el ratón se suelta, verificamos si la ficha está sobre un movimiento válido
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

                if(movimientoSeleccionado.capturas.Count > 0) {
                    foreach (var captura in movimientoSeleccionado.capturas) {
                        GameObject casilla = tableroComponente.getTablero()[captura.Item1, captura.Item2];
                        Casilla casillaComponente = casilla.GetComponent<Casilla>();

                        Ficha fichaCapturada = casillaComponente.getFicha();
                        casillaComponente.liberarCasilla();
                        if (fichaCapturada != null) {
                            Destroy(fichaCapturada.gameObject);
                        }
                    }
                }

                tableroComponente.cambiarTurno();
            } else {
                transform.position = posicionOriginal;
            }

            // Restauramos el estado de la ficha
            estaSiendoArrastrada = false;
            fichaRenderer.material.color = colorOriginal;
        }
    }

    public void fichaBlanca() {
        esBlanca = true;
    }

    public bool esFichaBlanca() {
        return esBlanca;
    }

    public void setPosicion(int fila, int columna) {
        posicionFila = fila;
        posicionColumna = columna;
    }

    void calcularMovimientosPosibles(int posicionFila, int posicionColumna) {
        GameObject[,] tablero = tableroComponente.getTablero();

        int filaSiguiente = posicionFila + 1;
        int columnaAnterior = posicionColumna - 1;
        int columnaSiguiente = posicionColumna + 1;

        if(!esBlanca) {
            filaSiguiente = posicionFila - 1;
        }

        // Comprobamos que las casillas sean válidas y no estén ocupadas
        if (filaSiguiente < 10) {
            if (columnaAnterior >= 0) {
                bool estaOcupada = casillaOcupada(filaSiguiente, columnaAnterior);

                if (!estaOcupada) {
                    // Resaltamos la casilla azul si es válida para movimiento
                    tablero[filaSiguiente, columnaAnterior].GetComponent<Renderer>().material.color = Color.blue;
                    movimientos.Add(new Movimiento(filaSiguiente, columnaAnterior));
                } else {
                    // Si la casilla está ocupada, comprobamos si la ficha es del color contrario
                    comprobarComerFicha(tablero, (posicionFila, posicionColumna), (filaSiguiente, columnaAnterior), (esBlanca ? filaSiguiente + 1 : filaSiguiente - 1, columnaAnterior - 1) );
                }
            }

            if (columnaSiguiente < 10) {
                bool estaOcupada = casillaOcupada(filaSiguiente, columnaSiguiente);

                if (!estaOcupada) {
                    // Resaltamos la casilla azul si es válida para movimiento
                    movimientos.Add(new Movimiento(filaSiguiente, columnaSiguiente));
                } else {
                    // Si la casilla está ocupada, comprobamos si la ficha es del color contrario
                    comprobarComerFicha(tablero, (posicionFila, posicionColumna), (filaSiguiente, columnaSiguiente),( esBlanca ? filaSiguiente + 1 : filaSiguiente - 1, columnaSiguiente + 1) );
                }
            }
        }
    }

    void comprobarComerFicha(GameObject[,] tablero, (int fila, int columna) posicionFicha, (int fila, int columna) ocupada, (int fila, int columna) libre, Movimiento movimientoAnterior = null) {
        if(ocupada.fila < 0 || ocupada.fila >= 10 || ocupada.columna < 0 || ocupada.columna >= 10) {
            return;
        }

        if(libre.fila < 0 || libre.fila >= 10 || ocupada.columna < 0 || ocupada.columna >= 10) {
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
}
