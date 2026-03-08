import FlightsPage from './features/flights/FlightsPage'
import TransportReservationSection from './features/reservations/TransportReservationSection'
import DesignPatternsDemoSection from './features/designpatterns/DesignPatternsDemoSection'
import './App.css'

function App() {
    return (
        <div>
            <FlightsPage />
            <TransportReservationSection />
            <DesignPatternsDemoSection />
        </div>
    );
}

export default App;
