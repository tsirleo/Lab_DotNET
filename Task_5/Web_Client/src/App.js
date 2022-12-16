import './App.css';
import "react-widgets/styles.css";
import 'bootstrap/dist/css/bootstrap.min.css';

import {
    ServerControllersApi
} from './api.js'

import {
    useState,
    useEffect
} from 'react';
import logo from './logo.svg';
import Combobox from "react-widgets/Combobox";
import DropdownList from "react-widgets/DropdownList";

import Container from 'react-bootstrap/Container';
import Col from 'react-bootstrap/Col';
import ListGroup from 'react-bootstrap/ListGroup';
import Row from 'react-bootstrap/Row';
import Tab from 'react-bootstrap/Tab';
import Card from 'react-bootstrap/Card';

import Image from 'react-bootstrap/Image';
import Button from 'react-bootstrap/Button';


function App() {
    const infoMessages = ["Data is loading from the database.", "Data has loaded from the database.", "Data has been deleted from the database."]
    const [value, setValue] = useState("happiness");
    const [results, setResults] = useState([]);
    const [info, setInfo] = useState(infoMessages[0]);
    const [timestamp, setTimestamp] = useState(new Date().toLocaleString('ru-RU'));
    const emotions = ["anger", "contempt", "disgust", "fear", "happiness", "neutral", "sadness", "surprise"];

    const controller = new ServerControllersApi();

    useEffect(() => {
        controller.imagesGet().then((result) => {
            if (results.length === 0) {
                setResults(result);
                setInfo(infoMessages[1]);
                setTimestamp(new Date().toLocaleString('ru-RU'));
            }
        }).catch(function (err) {
            setInfo("Error: " + err.message);
            setTimestamp(new Date().toLocaleString('ru-RU'));
        });
    }, []);

    const reloadDatabase = () => {
        setResults([]);
        setInfo(infoMessages[0]);
        controller.imagesGet().then((result) => {
            setResults(result);
            setInfo(infoMessages[1]);
            setTimestamp(new Date().toLocaleString('ru-RU'));
            console.log(result);
        }).catch(function (err) {
            setInfo("Error: " + err.message);
            setTimestamp(new Date().toLocaleString('ru-RU'));
        });
    }

    const dropDatabase = () => {
        controller.imagesDelete().then((result) => {
            setResults([]);
            setInfo(infoMessages[2]);
            setTimestamp(new Date().toLocaleString('ru-RU'));
            console.log(result);
        }).catch(function (err) {
            setInfo("Error: " + err.message);
            setTimestamp(new Date().toLocaleString('ru-RU'));
        });
    }

    const sortByEmo = (emo) => {
        results.sort(function (first, second) {
            return second.emotionsDict[emo] - first.emotionsDict[emo];
        });
    };
    sortByEmo(value);

    return (
        <div style={{ height: '100vh' }}>
            <Card className="mainCard">
                <Card.Header as="h5">Tsirleo EmoApp
                    <Button variant="outline-danger" style={{ float: "right", marginLeft: "1%" }} onClick={dropDatabase}>Delete Database</Button>
                    <Button variant="outline-primary" style={{ float: "right" }} onClick={reloadDatabase}>Load Database</Button>
                </Card.Header>
                <Card.Body>
                    <Card.Title></Card.Title>
                    <Container>
                        <Row>
                            <Col style={{ marginBottom: "1%" }} sm={5}><DropdownList
                                value={value}
                                onChange={(nextValue) => setValue(nextValue)
                                }
                                data={emotions} /></Col>
                            <Col>

                            </Col>
                        </Row>
                        <Row>
                            <Col></Col>
                        </Row>
                        <Row>
                            <Col><Tab.Container id="list-group-tabs-example" defaultActiveKey="#link0">
                                <Row>
                                    <Col sm={5}>
                                        <ListGroup className="list-group">
                                            {results && results.map((object, i) =>
                                                <ListGroup.Item key={i} action href={"#link" + i}>
                                                    <Image src={"data:image/jpeg;base64, " + object.image.blob} className='resize_fit_center' />
                                                </ListGroup.Item>)
                                            }
                                        </ListGroup>
                                    </Col>
                                    <Col sm={2}>
                                    </Col>
                                    <Col sm={5}>
                                        <Tab.Content style={{}}>
                                            {results && results.map((object, i) =>
                                                <Tab.Pane eventKey={"#link" + i}>
                                                    <Card className="">
                                                        <Card.Header as="h5" style={{ textAlign: "center" }}>Probability of emotion classes</Card.Header>
                                                        <Card.Body>
                                                            <h3 style={{ textAlign: "center" }}>{object.fileName}</h3>
                                                            <pre>{JSON.stringify(JSON.parse(object.emotionsJSON), null, 4)}</pre>
                                                        </Card.Body>
                                                    </Card>
                                                </Tab.Pane>

                                            )
                                            }
                                            <Card className="" style={{ marginTop: "5%" }}>
                                                <Card.Header as="h5" style={{ textAlign: "center" }}> Information </Card.Header>
                                                <Card.Body>
                                                    <pre style={{ maxWidth: "100%", overflowWrap: "break-word" }}>[{timestamp}] {info} </pre>
                                                </Card.Body>
                                            </Card>
                                        </Tab.Content>
                                    </Col>
                                </Row>
                            </Tab.Container>
                            </Col>
                        </Row>
                    </Container>
                </Card.Body>
            </Card>
        </div>
    );

}

export default App;
