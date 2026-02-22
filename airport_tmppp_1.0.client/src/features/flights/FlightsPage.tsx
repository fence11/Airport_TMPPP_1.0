import { useEffect, useState } from "react";
import { getFlights } from "../../api/flightsApi";
import type { Flight } from "./flightTypes";

const FlightsPage = () => {
    const [flights, setFlights] = useState<Flight[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        getFlights()
            .then(setFlights)
            .finally(() => setLoading(false));
    }, []);

    if (loading) return <p>Loading flights...</p>;

    return (
        <div>
            <h2>Flights</h2>
            <ul>
                {flights.map(flight => (
                    <li key={flight.id}>
                        Flight Num:{flight.flightNumber} (Airport ID: {flight.airportId})
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default FlightsPage;
