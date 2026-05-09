import FlightsPage from './features/flights/FlightsPage'
import TransportReservationSection from './features/reservations/TransportReservationSection'
import DesignPatternsDemoSection from './features/designpatterns/DesignPatternsDemoSection'
import StructuralPatternsDemoSection from './features/structuralpatterns/StructuralPatternsDemoSection'
import './App.css'

function App() {
    return (
        <div>
            <FlightsPage />
            <TransportReservationSection />
            <DesignPatternsDemoSection />
            <StructuralPatternsDemoSection />
        </div>
    );
}

export default App;
