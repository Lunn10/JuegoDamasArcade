using UnityEngine;

public class Casilla : MonoBehaviour
{
    int posicionColumna;
    int posicionFila;

    bool ocupada = false;
    bool fichaBlanca = false;
    Ficha ficha;
    
    public void setPosicion(int fila, int columna) {
        posicionColumna = columna;
        posicionFila = fila;
    }

    public bool getOcupada() {
        return ocupada;
    }
    

    public Ficha getFicha() {
        return ficha;
    }

    public void ocuparCasilla(bool ocupada) {
        this.ocupada = ocupada;
    }

    public bool esFichaBlanca() {
        return fichaBlanca;
    }

    public void liberarCasilla() {
        ocupada = false;
        fichaBlanca = false;
        ficha = null;
    }

    public void setFicha(Ficha ficha) {
        this.ficha = ficha;

        if(ficha.esFichaBlanca()) {
            setFichaBlanca();
        } else {
            setFichaNegra();
        }
    }

    public void setFichaBlanca() {
        fichaBlanca = true;
        ocuparCasilla(true);
    }

    public void setFichaNegra() {
        fichaBlanca = false;
        ocuparCasilla(true);
    }
}
