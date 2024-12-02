using UnityEngine;
using System;
using System.Collections.Generic;

public class GenerarTablero : MonoBehaviour {
    public GameObject casillaPrefab; // Prefab de la casilla (por ejemplo, un cubo o plano)
    public GameObject fichaPrefab;   // Prefab de la ficha (por ejemplo, una esfera o cilindro)
    private int filas = 10;            // Número de filas
    private int columnas = 10;         // Número de columnas
    private float distancia = 1f;     // Distancia entre casillas (puedes ajustarlo según el tamaño del prefab)
    private int cantidadFichas = 15;
    string turnoActual = "BLANCAS";
    private GameObject[,] tablero;
    private int fichasColocadasBlancas;
    private int fichasColocadasNegras;
    public Material materialNegro;
    public Material materialRojo;

    void Start() {
        // Llamar a la función para generar el tablero
        tablero = new GameObject[filas, columnas];
        CrearTablero();
        fichasColocadasBlancas = 0;
        fichasColocadasNegras = 0;
    }

    void CrearTablero() {
        // Recorremos las filas y columnas para instanciar cada casilla
        for (int filaActual = 0; filaActual < filas; filaActual++) {
            for (int columnaActual = 0; columnaActual < columnas; columnaActual++) {
                // Calculamos la posición en el eje X y Z (asegurándonos de que coincidan con el orden de la matriz)
                Vector3 posicion = new Vector3(columnaActual * distancia, 0, filaActual * distancia);  // Corregido aquí

                // Instanciamos el prefab de la casilla en la posición calculada
                GameObject casillaActual = Instantiate(casillaPrefab, posicion, Quaternion.identity, transform);
                casillaActual.GetComponent<Casilla>().setPosicion(filaActual, columnaActual);

                tablero[filaActual, columnaActual] = casillaActual;

                if(casillaRoja(filaActual, columnaActual)) {
                    casillaActual.GetComponent<Renderer>().material = materialRojo;
                    
                    if(filaActual <= 2 && fichasColocadasBlancas < cantidadFichas) {
                        GameObject ficha = Instantiate(fichaPrefab, posicion + new Vector3(0, 0.3f, 0), Quaternion.identity, transform);
                        ficha.GetComponent<Ficha>().setPosicion(filaActual, columnaActual);
                        ficha.GetComponent<Ficha>().fichaBlanca();
                        casillaActual.GetComponent<Casilla>().setFicha(ficha.GetComponent<Ficha>());
                        fichasColocadasBlancas++;
                    } else if(filaActual >= 7 && fichasColocadasNegras < cantidadFichas) {
                        GameObject ficha = Instantiate(fichaPrefab, posicion + new Vector3(0, 0.3f, 0), Quaternion.identity, transform);
                        ficha.GetComponent<Renderer>().material = materialNegro;
                        ficha.GetComponent<Ficha>().setPosicion(filaActual, columnaActual);
                        casillaActual.GetComponent<Casilla>().setFicha(ficha.GetComponent<Ficha>());
                        fichasColocadasNegras++;                        
                    }
                }
            }
        }
    }

    public string getTurno() {
        return turnoActual;
    }

    public void cambiarTurno() {
        if(turnoActual == "BLANCAS") {
            turnoActual = "NEGRAS";
        } else {
            turnoActual = "BLANCAS";
        }
    }

    public GameObject[,] getTablero() {
        return tablero;
    }

    public void resaltarCasillas(List<Tuple<int, int>> casillas) {
        Debug.Log("Casillas resaltadas:");
        Debug.Log(casillas.ToArray());
    }

    bool casillaRoja(int x, int z) {
        if ((x + z) % 2 == 0) {
            return true;
        }

        return false;
    }

    void mostrarTablero() {
        for (int i = 0; i < filas; i++) {
            string fila = "";
            for (int j = 0; j < columnas; j++) {
                fila += tablero[i, j] + " ";
            }

            Debug.Log(fila); 
        }
    }
}
