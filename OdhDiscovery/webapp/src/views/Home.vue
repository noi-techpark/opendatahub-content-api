<template>
  <div class="home">
    <div class="container">
      <div class="hero">
        <h1>Open Data Hub Discovery</h1>
        <p class="hero-subtitle">
          Explore datasets and timeseries from the Open Data Hub platform
        </p>
      </div>

      <div class="features grid grid-2">
        <router-link to="/datasets" class="feature-card card">
          <h2>Browse Datasets</h2>
          <p>
            Explore static content and metadata about tourism entities in South Tyrol.
            View accommodations, activities, events, articles, and more.
          </p>
          <ul>
            <li>View dataset structure and completeness</li>
            <li>Filter and search entries</li>
            <li>Analyze field coverage and examples</li>
            <li>Discover timeseries attachments</li>
          </ul>
          <span class="cta">Explore Datasets →</span>
        </router-link>

        <router-link to="/timeseries" class="feature-card card">
          <h2>Browse Timeseries</h2>
          <p>
            Discover and inspect time-series measurement data from sensors.
            Temperature, humidity, air quality, and geographic data.
          </p>
          <ul>
            <li>Explore available measurement types</li>
            <li>Discover sensors and their capabilities</li>
            <li>View historical measurements</li>
            <li>Visualize data on charts and maps</li>
          </ul>
          <span class="cta">Explore Timeseries →</span>
        </router-link>
      </div>

      <div class="info-section">
        <div class="card">
          <h2>About This Tool</h2>
          <p>
            The Open Data Hub Discovery application helps developers, data analysts, and
            maintainers understand and explore the data available in the Open Data Hub platform.
          </p>
          <div class="capabilities">
            <div class="capability">
              <h3>Dataset Exploration</h3>
              <p>
                Browse 20+ different dataset types including accommodations, activities, POIs, events,
                and more. Each dataset can be inspected with detailed statistics about data completeness,
                field structure, and examples.
              </p>
            </div>
            <div class="capability">
              <h3>Timeseries Analysis</h3>
              <p>
                Discover sensors and measurement types. View real-time and historical data.
                Visualize numeric data on charts and geographic data on maps with time-based playback.
              </p>
            </div>
            <div class="capability">
              <h3>API Integration</h3>
              <p>
                Every view generates the corresponding API call (cURL format) for easy integration.
                All filters and configurations are persisted in the URL for sharing and reproducibility.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
// Home page
</script>

<style scoped>
.home {
  padding: 2rem 0;
}

.hero {
  text-align: center;
  padding: 3rem 0;
}

.hero h1 {
  font-size: 3rem;
  font-weight: 800;
  background: linear-gradient(135deg, var(--primary-color), #7c3aed);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 1rem;
}

.hero-subtitle {
  font-size: 1.25rem;
  color: var(--text-secondary);
  max-width: 600px;
  margin: 0 auto;
}

.features {
  margin: 3rem 0;
}

.feature-card {
  text-decoration: none;
  color: inherit;
  transition: all 0.3s ease;
  cursor: pointer;
  position: relative;
  overflow: hidden;
}

.feature-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 4px;
  background: linear-gradient(90deg, var(--primary-color), #7c3aed);
  transform: scaleX(0);
  transition: transform 0.3s ease;
}

.feature-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
}

.feature-card:hover::before {
  transform: scaleX(1);
}

.feature-card h2 {
  font-size: 1.5rem;
  margin-bottom: 1rem;
  color: var(--text-primary);
}

.feature-card p {
  color: var(--text-secondary);
  margin-bottom: 1rem;
  line-height: 1.6;
}

.feature-card ul {
  list-style: none;
  margin: 1.5rem 0;
}

.feature-card li {
  padding: 0.5rem 0;
  padding-left: 1.5rem;
  position: relative;
  color: var(--text-secondary);
}

.feature-card li::before {
  content: '✓';
  position: absolute;
  left: 0;
  color: var(--success-color);
  font-weight: 700;
}

.cta {
  display: inline-block;
  margin-top: 1rem;
  color: var(--primary-color);
  font-weight: 600;
  transition: transform 0.2s ease;
}

.feature-card:hover .cta {
  transform: translateX(4px);
}

.info-section {
  margin: 3rem 0;
}

.info-section h2 {
  font-size: 1.75rem;
  margin-bottom: 1rem;
  color: var(--text-primary);
}

.info-section > .card > p {
  color: var(--text-secondary);
  line-height: 1.6;
  margin-bottom: 2rem;
}

.capabilities {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 2rem;
  margin-top: 2rem;
}

.capability h3 {
  font-size: 1.125rem;
  margin-bottom: 0.5rem;
  color: var(--primary-color);
}

.capability p {
  color: var(--text-secondary);
  font-size: 0.875rem;
  line-height: 1.6;
}
</style>
