import { Component, Input, OnChanges, AfterViewInit, ElementRef, ViewChild } from '@angular/core';
import * as L from 'leaflet';

@Component({
  selector: 'app-result-map',
  standalone: true,
  template: `<div #mapContainer class="map-container"></div>`,
  styles: [`.map-container { height: 450px; width: 100%; border-radius: 8px; }`],
})
export class ResultMapComponent implements OnChanges, AfterViewInit {
  @ViewChild('mapContainer') mapElement!: ElementRef;
  @Input() rows: Record<string, any>[] = [];

  private map: L.Map | null = null;
  private initialized = false;

  ngAfterViewInit(): void {
    this.initialized = true;
    this.initMap();
  }

  ngOnChanges(): void {
    if (this.initialized) this.updateMarkers();
  }

  private initMap(): void {
    this.map = L.map(this.mapElement.nativeElement).setView([52.1, 5.5], 7);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(this.map);
    this.updateMarkers();
  }

  private updateMarkers(): void {
    if (!this.map) return;

    // Clear existing markers
    this.map.eachLayer(layer => {
      if (layer instanceof L.CircleMarker) this.map!.removeLayer(layer);
    });

    const maxCount = Math.max(...this.rows.map(r => r['Aantal'] || 1));

    for (const row of this.rows) {
      const lat = row['Lat'];
      const lng = row['Lng'];
      if (!lat || !lng) continue;

      const radius = Math.max(8, (row['Aantal'] / maxCount) * 30);
      L.circleMarker([lat, lng], {
        radius,
        fillColor: '#2c5282',
        fillOpacity: 0.6,
        color: '#1a365d',
        weight: 1,
      })
        .bindPopup(`<b>${row['Stad']}</b><br>Aantal: ${row['Aantal']}`)
        .addTo(this.map);
    }
  }
}
