using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public class Movimiento {
    public int fila { get; set; }
    public int columna { get; set; }
    public List<Tuple<int, int>> capturas { get; set; }

    public Movimiento(int fila, int columna) {
        this.fila = fila;
        this.columna = columna;
        capturas = new List<Tuple<int, int>>();
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

                        // Obtener la ficha asociada a la casilla y destruirla
                        Ficha fichaCapturada = casillaComponente.getFicha();
                        tableroComponente.getTablero()[fichaCapturada.posicionFila, fichaCapturada.posicionColumna].GetComponent<Casilla>().liberarCasilla();
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
                    comprobarComerFicha(tablero, filaSiguiente, columnaAnterior, esBlanca ? filaSiguiente + 1 : filaSiguiente - 1, columnaAnterior - 1 );
                }
            }

            if (columnaSiguiente < 10) {
                bool estaOcupada = casillaOcupada(filaSiguiente, columnaSiguiente);

                if (!estaOcupada) {
                    // Resaltamos la casilla azul si es válida para movimiento
                    tablero[filaSiguiente, columnaSiguiente].GetComponent<Renderer>().material.color = Color.blue;
                    movimientos.Add(new Movimiento(filaSiguiente, columnaSiguiente));
                } else {
                    // Si la casilla está ocupada, comprobamos si la ficha es del color contrario
                    comprobarComerFicha(tablero, filaSiguiente, columnaSiguiente, esBlanca ? filaSiguiente + 1 : filaSiguiente - 1, columnaSiguiente + 1 );
                }
            }
        }
    }

    void comprobarComerFicha(GameObject[,] tablero, int filaOcupada, int columnaOcupada, int filaLibre, int columnaLibre) {
        if(filaLibre < 0 || filaLibre >= 10 || columnaLibre < 0 || columnaLibre >= 10) {
            return;
        }
        
        bool esFichaBlanca = tablero[filaOcupada, columnaOcupada].GetComponent<Casilla>().esFichaBlanca();

        if (esBlanca != esFichaBlanca && !casillaOcupada(filaLibre, columnaLibre)) {
            tablero[filaLibre, columnaLibre].GetComponent<Renderer>().material.color = Color.blue;
            Movimiento movimiento = new Movimiento(filaLibre, columnaLibre);
            movimiento.capturas.Add(new Tuple<int, int>(filaOcupada, columnaOcupada));
            movimientos.Add(movimiento);
        }
    }

    bool casillaOcupada(int fila, int columna) {
        GameObject[,] tablero = tableroComponente.getTablero();
        return tablero[fila, columna].GetComponent<Casilla>().getOcupada();
    }

    void limpiarMovimientosPosibles() {
        // Limpiamos los movimientos posibles (restaurando color de las casillas)
        GameObject[,] tablero = tableroComponente.getTablero();
        foreach (var movimiento in movimientos) {
            tablero[movimiento.fila, movimiento.columna].GetComponent<Renderer>().material.color = Color.red;
        }
        movimientos.Clear();
    }
}
